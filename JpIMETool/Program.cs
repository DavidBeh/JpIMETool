using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.Extensions.Reflection;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32.VK;

namespace JpIMTool;

class Program
{
    private static List<KeyOverride> _keyOverrides = new()
    {
        new KeyOverride((ushort)VK_OEM_5, (ushort)IMEKeys.HalfAlphaToggle, Alt: true),
        new KeyOverride((ushort)VK_CAPITAL, (ushort)IMEKeys.Hiragana, Control: true),
        new KeyOverride((ushort)VK_CAPITAL, (ushort)IMEKeys.Katakana, Alt: true),
        new KeyOverride((ushort)VK_CAPITAL, (ushort)IMEKeys.Eisu, Shift: true),
    };

    [STAThread]
    static void Main(string[] args)
    {
        User32.HookProc proc = HookCallback;
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-loadkeyboardlayouta
        // hkl = User32.LoadKeyboardLayout("00000407", User32.KLF.KLF_NOTELLSHELL);
        var threadId = Kernel32.GetCurrentThreadId();
        var t = new Thread(async () =>
        {
            await Task.Delay(2000);
            while (true)
            {

                Console.WriteLine("Press r to rehook");
                var key = Console.ReadKey();
                
                if (key.Key == ConsoleKey.R)
                {
                    User32.PostThreadMessage(threadId, (uint)User32.WindowMessage.WM_QUIT,0,0);
                }
            }
        });

        t.Start();
        
        
        
        while (true)
        {
            using var handle = User32.SetWindowsHookEx(User32.HookType.WH_KEYBOARD_LL, proc,
                Kernel32.GetModuleHandle(null),
                0);
            Console.WriteLine("Hooked: " + handle + ". Starting message loop");
            /*
            Application.Run(new ApplicationContext());*/
            while(true)
            {
                
                var bRet = (User32.GetMessage(out var msg, IntPtr.Zero, 0, 0));
                if (bRet == 0) break;
                if (bRet == -1)
                {
                    // handle the error and possibly exit
                }
                else
                {
                    User32.TranslateMessage(msg); 
                    User32.DispatchMessage(msg); 
                }
            }            
            var result = User32.UnhookWindowsHookEx(handle);
            Console.WriteLine("Unhooked: " + result);
        }

    }
    
    
    
    private static IntPtr HookCallback(int ncode, IntPtr wparam, IntPtr lparam)
    {
        try
        {
            if (ncode < 0)
            {
                return User32.CallNextHookEx(IntPtr.Zero, ncode, wparam, lparam);
            }

            User32.KBDLLHOOKSTRUCT kbd = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lparam);

            bool isInjected = (kbd.flags & LLKHF_INJECTED) == LLKHF_INJECTED;

            var pressedKey = (User32.VK)kbd.vkCode;

            if (isInjected)
            {
                return User32.CallNextHookEx(IntPtr.Zero, ncode, wparam, lparam);
            }

            KeyEvent keyEvent = (KeyEvent)wparam;

            /*
            if (pressedKey == VK_K)
            {
                User32.INPUT a = new User32.INPUT();
                a.type = User32.INPUTTYPE.INPUT_KEYBOARD;
                // https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
                a.ki = a.ki with
                {
                    wVk = (ushort)VK_KANJI,
                    //wVk = newKey.Value,
                    dwFlags = keyEvent == KeyEvent.KeyDown ? 0 : User32.KEYEVENTF.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                    wScan = 0,
                };

                var arr = new[] { a };
                // Prinz size of INPUT struct
                //Console.WriteLine(Marshal.SizeOf<User32.INPUT>());

                var ret = User32.SendInput((uint)arr.Length, arr, Marshal.SizeOf<User32.INPUT>());
                return 1;
            }
            */


            bool isShift = User32.GetKeyState((int)VK_SHIFT).IsPressed();
            bool isControl = User32.GetKeyState((int)VK_CONTROL).IsPressed();
            bool isAlt = User32.GetKeyState((int)VK_MENU).IsPressed();
            bool isWindows = User32.GetKeyState((int)VK_LWIN).IsPressed() ||
                             User32.GetKeyState((int)VK_RWIN).IsPressed();
            ushort? newKey =
                _keyOverrides.FirstOrDefault(ko => ko.Check(isControl, isShift, isAlt, isWindows, (ushort)pressedKey))
                    ?.NewKeyVC;


            User32.HKL? hkl = null;
            bool keyIntercepted = false;
            if (newKey != null)
            {
                var foregroundWindow = User32.GetForegroundWindow();
                var threadId = User32.GetWindowThreadProcessId(foregroundWindow, out var processId);
                hkl = User32.GetKeyboardLayout(threadId);

                if (hkl.Value.LangId.Value == 1041) // Japanese
                {
                    User32.INPUT a = new User32.INPUT();
                    a.type = User32.INPUTTYPE.INPUT_KEYBOARD;
                    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
                    a.ki = a.ki with
                    {
                        wVk = newKey.Value,
                        //wVk = newKey.Value,
                        dwFlags = keyEvent == KeyEvent.KeyDown | keyEvent == KeyEvent.SysKeyDown
                            ? 0
                            : User32.KEYEVENTF.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                        wScan = 0,
                    };

                    var arr = new[] { a };
                    // Prinz size of INPUT struct
                    //Console.WriteLine(Marshal.SizeOf<User32.INPUT>());

                    var ret = User32.SendInput((uint)arr.Length, arr, Marshal.SizeOf<User32.INPUT>());
                    if (ret == 0)
                    {
                        Task.Run(() => Console.WriteLine(Marshal.GetLastWin32Error()));
                    }

                    keyIntercepted = true;
                }
            }


            Task.Run(() =>
            {
                Console.WriteLine(
                    $"K:{pressedKey,-15} E:{keyEvent,-15} S:{isShift,-5} C:{isControl,-5} A:{isAlt,-5} W:{isWindows,-5}, I:{isInjected,-5} NK:{(User32.VK?)newKey,-15} HKL:{(hkl.HasValue ? hkl.Value.DeviceId + " " + hkl.Value.LangId : " ")}");
            });

            if (newKey != null && keyIntercepted)
                return 1;

            return User32.CallNextHookEx(IntPtr.Zero, ncode, wparam, lparam);
        }
        catch (Exception e)
        {
            Task.Run(() => Console.WriteLine("Unexpected Exception in Hook Callback: " + e));
            return User32.CallNextHookEx(IntPtr.Zero, ncode, wparam, lparam);
        }
    }


    const long LLKHF_INJECTED = 0x00000010;

    enum KeyEvent
    {
        KeyDown = 0x0100,
        KeyUp = 0x0101,
        SysKeyDown = 0x0104,
        SysKeyUp = 0x0105
    }


    /*
     * If the high-order bit is 1, the key is down; otherwise, it is up.
     * If the low-order bit is 1, the key is toggled
     */
    public static (bool down, bool toogled) DecodeKeyState(short state)
    {
        return ((state & 0x8000) == 0x8000, (state & 0x0001) == 0x0001);
    }
}

record KeyOverride(
    ushort OriginalKeyVC,
    ushort NewKeyVC,
    bool Shift = false,
    bool Control = false,
    bool Alt = false,
    bool Windows = false)
{
    public bool Check(bool control, bool shift, bool alt, bool windows, ushort key)
    {
        return Control == control && Shift == shift && Alt == alt && Windows == windows && OriginalKeyVC == key;
    }
}

public enum IMEKeys
{
    /// <summary>
    /// VK_KANJI
    /// </summary>
    HalfAlphaToggle = 25,

    /// <summary>
    /// VK_OEM_COPY
    /// </summary>
    Hiragana = 242,

    /// <summary>
    /// VK_OEM_FINISH
    /// </summary>
    Katakana = 241,

    /// <summary>
    /// VK_OEM_ATTN
    /// </summary>
    Eisu = 240,

    /// <summary>
    /// VK_IME_ON
    /// </summary>
    IMEOn = 22,

    /// <summary>
    /// VK_IME_OFF
    /// </summary>
    OMEOff = 26
}

public static class ExtensionMethods
{
    public static bool IsPressed(this short state) => (state & 0x8000) == 0x8000;
}