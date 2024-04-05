This program enables the IME shortcuts for the Japanese IME in Windows 10+ with german or other keyboard layout
substitution.

This means you can use Japanese IME with working shortcuts with other keyboard layouts than US QWERTY

## Usage

* Download the latest release.
* Extract the folder to a location of your choice
* Make sure that the Japanese Keyboard Layout is installed with the Microsoft IME on your system. It only supports
  Japanese and English Keyboard Layouts
* Edit (follow the instructions inside the file) and then run the `Registry Scripts/create keyboard substute.reg` file
* Log out of your windows session and log in again or restart your computer
    * You will now have two selectable keyboard layouts in the taskbar for the Japanese language. You will generally
      want to use the one with the (J) symbol
* Start the `JpIMETool.exe` program in order to make keyboard shortcuts work with the Japanese IME

### More usage notes

* If you use other keyboard languages, you can switch between them using `Alt + Shift` instead of `Win + Space` in order
  switch between without selecting the additional (useless) keyboard layout option from the substitution operation
* You can adjust this shortcut in `Settings > Time & Language > Input > Advanced Keyboard Settings`
    * Or by
      running `"C:\Windows\system32\rundll32.exe" Shell32.dll,Control_RunDLL input.dll,,{C07337D3-DB2C-4D0B-9A93-B722A6C106E2}{HOTKEYS}`
      in the Windows Run Menu (`Win + R`)
* Refer to here for more information on the Japanese IME
  shortcuts: https://support.microsoft.com/en-us/windows/microsoft-japanese-ime-da40471d-6b91-4042-ae8b-713a96476916
* You can revert the keyboard layout substitution by running the `Registry Scripts/delete keyboard substitute.reg` file
  and then logging out and in again or restarting your computer