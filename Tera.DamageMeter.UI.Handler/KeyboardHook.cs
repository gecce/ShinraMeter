﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Tera.Data;

namespace Tera.DamageMeter.UI.Handler
{
    public sealed class KeyboardHook : IDisposable
    {
        private static KeyboardHook _instance;


        private readonly Window _window = new Window();
        private int _currentId;

        private IDpsWindow _dpswindow;

        private KeyboardHook()
        {
            // register the event of the inner native window.
            _window.KeyPressed += delegate(object sender, KeyPressedEventArgs args) { KeyPressed?.Invoke(this, args); };
        }

        public static KeyboardHook Instance => _instance ?? (_instance = new KeyboardHook());

        #region IDisposable Members

        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (var i = _currentId; i > 0; i--)
            {
                UnregisterHotKey(_window.Handle, i);
            }

            // dispose the inner native window.
            _window.Dispose();
        }

        #endregion

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.Key == BasicTeraData.Instance.HotkeysData.Paste.Key &&
                e.Modifier == BasicTeraData.Instance.HotkeysData.Paste.Value)
            {
                var text = Clipboard.GetText();
                Thread pasteThread = new Thread(() => CopyPaste.Paste(text));
                pasteThread.Start();
            }
            else if (e.Key == BasicTeraData.Instance.HotkeysData.Reset.Key &&
                     e.Modifier == BasicTeraData.Instance.HotkeysData.Reset.Value)
            {
                UiModel.Instance.Reset();
            }
            foreach (
                var copy in
                    BasicTeraData.Instance.HotkeysData.Copy.Where(
                        copy => e.Key == copy.Key && e.Modifier == copy.Modifier))
            {
                CopyPaste.Copy(_dpswindow.PlayerData(), copy.Header, copy.Content, copy.Footer, copy.OrderBy, copy.Order);
                var text = Clipboard.GetText();
                Console.WriteLine(text);
                Thread pasteThread = new Thread(() => CopyPaste.Paste(text));
                pasteThread.Start();
            }
        }

        public void RegisterKeyboardHook(IDpsWindow window)
        {
            _dpswindow = window;
            // register the event that is fired after the key press.
            Instance.KeyPressed += hook_KeyPressed;
            // register the control + alt + F12 combination as hot key.
            try
            {
                Instance.RegisterHotKey(BasicTeraData.Instance.HotkeysData.Paste.Value,
                    BasicTeraData.Instance.HotkeysData.Paste.Key);
                Instance.RegisterHotKey(BasicTeraData.Instance.HotkeysData.Reset.Value,
                    BasicTeraData.Instance.HotkeysData.Reset.Key);
            }
            catch
            {
                MessageBox.Show("Cannot bind paste/reset keys");
            }

            try
            {
                foreach (var copy in BasicTeraData.Instance.HotkeysData.Copy)
                {
                    Instance.RegisterHotKey(copy.Modifier, copy.Key);
                }
            }
            catch
            {
                MessageBox.Show("Cannot bind copy keys");
            }
        }

        // Registers a hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // Unregisters the hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        ///     Registers a hot key in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot key.</param>
        /// <param name="key">The key itself that is associated with the hot key.</param>
        public void RegisterHotKey(HotkeysData.ModifierKeys modifier, Keys key)
        {
            // increment the counter.
            _currentId = _currentId + 1;

            // register the hot key.
            if (!RegisterHotKey(_window.Handle, _currentId, (uint) modifier, (uint) key))
                throw new InvalidOperationException("Couldn’t register the hot key.");
        }

        /// <summary>
        ///     A hot key has been pressed.
        /// </summary>
        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        /// <summary>
        ///     Represents the window that is used internally to get the messages.
        /// </summary>
        private sealed class Window : NativeWindow, IDisposable
        {
            private static readonly int WmHotkey = 0x0312;

            public Window()
            {
                // create the handle for the window.
                CreateHandle(new CreateParams());
            }

            #region IDisposable Members

            public void Dispose()
            {
                DestroyHandle();
            }

            #endregion

            /// <summary>
            ///     Overridden to get the notifications.
            /// </summary>
            /// <param name="m"></param>
            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                base.WndProc(ref m);

                // check if we got a hot key pressed.
                if (m.Msg == WmHotkey)
                {
                    // get the keys.
                    var key = (Keys) (((int) m.LParam >> 16) & 0xFFFF);
                    var modifier = (HotkeysData.ModifierKeys) ((int) m.LParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    KeyPressed?.Invoke(this, new KeyPressedEventArgs(modifier, key));
                }
            }

            public event EventHandler<KeyPressedEventArgs> KeyPressed;
        }
    }

    /// <summary>
    ///     Event Args for the event that is fired after the hot key has been pressed.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        internal KeyPressedEventArgs(HotkeysData.ModifierKeys modifier, Keys key)
        {
            Modifier = modifier;
            Key = key;
        }

        public HotkeysData.ModifierKeys Modifier { get; }

        public Keys Key { get; }
    }
}