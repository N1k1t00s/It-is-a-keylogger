using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Clipboard
{
    internal static class NativeMethods
    {
        //Reference https://docs.microsoft.com/en-us/windows/desktop/dataxchg/wm-clipboardupdate
        //Отправляется при изменении содержимого буфера обмена.
        public const int WM_CLIPBOARDUPDATE = 0x031D;

        //Reference https://www.pinvoke.net/default.aspx/Constants.HWND
        //Константы HWND (из winuser.h)
        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        //Reference https://www.pinvoke.net/default.aspx/user32/AddClipboardFormatListener.html
        // Объявление метода API для помещения данного окна в список слушателей формата буфера обмена, поддерживаемого системой.
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        //Reference https://www.pinvoke.net/default.aspx/user32.setparent
        //Изменяет родительское окно указанного дочернего окна.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        //Reference https://www.pinvoke.net/default.aspx/user32/getwindowtext.html
        //Копирует текст строки заголовка указанного окна (если она есть) в буфер.
        //Если указанное окно является элементом управления, копируется текст элемента управления.
        //Однако GetWindowText не может получить текст элемента управления в другом приложении,
        //Если целевое окно принадлежит текущему процессу, GetWindowText вызывает отправку сообщения WM_GETTEXT указанному окну
        //или элементу управления. 
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        //Reference https://www.pinvoke.net/default.aspx/user32.getwindowtextlength
        //Возвращает длину текста элемента управления.
        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        //Reference https://www.pinvoke.net/default.aspx/user32.getforegroundwindow
        //Возвращает текущее окно, с которым работает пользователь.
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }

    public static class Clipboard
    {
        public static string GetText()
        {
            string ReturnValue = string.Empty;
            Thread STAThread = new Thread(
                delegate ()
                {
                    ReturnValue = System.Windows.Forms.Clipboard.GetText();
                });
            STAThread.SetApartmentState(ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return ReturnValue;
        }
    }

    public sealed class ClipboardNotification
    {
        public class NotificationForm : Form
        {
            string lastWindow = "";

            public NotificationForm()
            {
                //Превращает дочернее окно в окно только для сообщений (см. документацию Microsoft)
                NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
                //Помещает окно в список "слушателей" формата буфера обмена, поддерживаемого системой
                NativeMethods.AddClipboardFormatListener(Handle);
            }

            protected override void WndProc(ref Message m)
            {
                //Слежка за сообщениями операционной системы
                if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {

                    //Запись в stdout активного окна
                    IntPtr active_window = NativeMethods.GetForegroundWindow();
                    int length = NativeMethods.GetWindowTextLength(active_window);
                    StringBuilder sb = new StringBuilder(length + 1);
                    NativeMethods.GetWindowText(active_window, sb, sb.Capacity);
                    Trace.WriteLine("");
                    Trace.WriteLine("\t[cntrl-C] Clipboard Copied: " + Clipboard.GetText());
                }
                //Вызывается для любых необработанных сообщений
                base.WndProc(ref m);
            }
        }

    }
}