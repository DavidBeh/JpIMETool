using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;
using Windows.Win32;
using Vanara.Extensions;
using Vanara.PInvoke;


namespace IMEKeyboardLayout;

class Program
{
    static User32.SafeHKL? hkl;

    [STAThread]
    static void Main(string[] args)
    {
        User32.HookProc proc = HookCallback;
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-loadkeyboardlayouta
        hkl = User32.LoadKeyboardLayout("00000407", User32.KLF.KLF_NOTELLSHELL);
        User32.SetWindowsHookEx(User32.HookType.WH_KEYBOARD_LL, proc, Kernel32.GetModuleHandle(null), 0);
        Application.Run();


        Console.WriteLine("Hello, World!");
    }

    // Eats presses to x and y and calls SendInput to simulate a swap of x and y
    // https://stackoverflow.com/questions/27303878/when-i-swap-keys-using-setwindowshookex-wh-keyboard-ll-why-does-my-program-get
    private static IntPtr HookCallback(int ncode, IntPtr wparam, IntPtr lparam)
    {
        User32.KBDLLHOOKSTRUCT kbd = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lparam);
        // wparam 
        KeyEvent keyEvent = (KeyEvent)wparam;


        // get if injected
        bool injected = (kbd.flags & LLKHF_INJECTED) == LLKHF_INJECTED;
        var pressedKey = (User32.VK)kbd.vkCode;

        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mapvirtualkeyexa

        short isShift = User32.GetKeyState((int)User32.VK.VK_SHIFT);
        short isLShift = User32.GetKeyState((int)User32.VK.VK_LSHIFT);
        short isCaps = User32.GetKeyState((int)User32.VK.VK_CAPITAL);
        short isNum = User32.GetKeyState((int)User32.VK.VK_NUMLOCK);
        short isCtrl = User32.GetKeyState((int)User32.VK.VK_CONTROL);
        Console.WriteLine(
            $"Key: {pressedKey} Event: {keyEvent} Injected: {injected} Caps: {DecodeKeyState(isCaps)} LShift: {DecodeKeyState(isLShift)} Num: {isNum} Ctrl: {isCtrl}");

        User32.VK? subst = injected ? null : GetSubstiution(pressedKey);
        if (subst != null)
        {
            User32.INPUT a = new User32.INPUT();
            a.type = User32.INPUTTYPE.INPUT_KEYBOARD;


            // https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
            a.ki = a.ki with
            {
                wVk = (ushort)subst,
                dwFlags = keyEvent == KeyEvent.KeyDown ? 0 : User32.KEYEVENTF.KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = IntPtr.Zero,
                wScan = 0,
            };


            var arr = new[] { a };
            // Prinz size of INPUT struct
            //Console.WriteLine(Marshal.SizeOf<User32.INPUT>());

            var ret = User32.SendInput((uint)arr.Length, arr, Marshal.SizeOf<User32.INPUT>());
            Console.WriteLine(ret);
            // print error
            return 1;
        }


        return User32.CallNextHookEx(IntPtr.Zero, ncode, wparam, lparam);
    }

    static User32.VK? GetSubstiution(User32.VK key) =>
        key switch
        {
            User32.VK.VK_T => User32.VK.VK_KANJI,
            User32.VK.VK_H => User32.VK.VK_OEM_COPY,
            User32.VK.VK_K => User32.VK.VK_OEM_FINISH,
            User32.VK.VK_E => User32.VK.VK_OEM_ATTN,
            User32.VK.VK_A => User32.VK.VK_IME_ON,
            User32.VK.VK_B => User32.VK.VK_IME_OFF,
            _ => null
        };

    const long LLKHF_INJECTED = 0x00000010;

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