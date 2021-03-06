﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DamageMeter.AutoUpdate;
using DamageMeter.Sniffing;
using DamageMeter.UI.EntityStats;
using Data;
using log4net;
using Application = System.Windows.Forms.Application;
using ContextMenu = System.Windows.Forms.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using System.Net.Http;
using System.Text;

namespace DamageMeter.UI
{
    /// <summary>
    ///     Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly EntityStatsMain _entityStats;
        private bool _keyboardInitialized;

        private NotifyIcon _trayIcon;

        public MainWindow()
        {
         
               
            InitializeComponent();
            var currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            // Handler for exceptions in threads behind forms.
            Application.ThreadException += GlobalThreadExceptionHandler;
            if (BasicTeraData.Instance.WindowData.InvisibleUI)
            {
                Visibility = Visibility.Hidden;
            }
            App.Current.Resources["Scale"] = BasicTeraData.Instance.WindowData.Scale;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
            TeraSniffer.Instance.Enabled = true;
            NetworkController.Instance.Connected += HandleConnected;
            NetworkController.Instance.TickUpdated += Update;
            NetworkController.Instance.SetClickThrouAction += SetClickThrou;
            NetworkController.Instance.UnsetClickThrouAction += UnsetClickThrou;
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += UpdateKeyboard;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            PinImage.Source = BasicTeraData.Instance.ImageDatabase.UnPin.Source;
            EntityStatsImage.Source = BasicTeraData.Instance.ImageDatabase.EntityStats.Source;
            Chrono.Source = BasicTeraData.Instance.ImageDatabase.Chrono.Source;
            CloseWindow.Source = BasicTeraData.Instance.ImageDatabase.Close.Source;
            History.Source = BasicTeraData.Instance.ImageDatabase.History.Source;
            Config.Source = BasicTeraData.Instance.ImageDatabase.Config.Source;
            this.Chatbox.Source = BasicTeraData.Instance.ImageDatabase.Chat.Source;
            ListEncounter.PreviewKeyDown += ListEncounterOnPreviewKeyDown;
            UpdateComboboxEncounter(new LinkedList<KeyValuePair<Entity,EntityInfo>>(), null);
            Title = "Shinra Meter V" + UpdateManager.Version;
            BackgroundColor.Opacity = BasicTeraData.Instance.WindowData.MainWindowOpacity;
            Closing -= ClickThrouWindow_Closing;
            Topmost = BasicTeraData.Instance.WindowData.Topmost;
            _entityStats = new EntityStatsMain(this);
            _chatbox = new Chatbox(new List<ChatMessage>());
            _windowHistory = new TeradpsHistory(new ConcurrentDictionary<string, Entity>());
            Console.WriteLine("SuperUniqueStringEasilyDetectableToBeAbleToRandomizeTheProgramAndBypassSignatureBasedBlock");
            TrayConfiguration();
        }
        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Exit();
        }

        private Chatbox _chatbox; 

        public void SetClickThrou()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
            foreach (var players in Controls)
            {
                players.Value.SetClickThrou();
            }
            _entityStats.SetClickThrou();
            _clickThrou.Text = "Desactivate click throu";
            EntityStatsImage.Source = BasicTeraData.Instance.ImageDatabase.EntityStatsClickThrou.Source;
        }

        public void UnsetClickThrou()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExVisible(hwnd);
            foreach (var players in Controls)
            {
                players.Value.UnsetClickThrou();
            }
            _entityStats.UnsetClickThrou();
            _clickThrou.Text = "Activate click throu";
            EntityStatsImage.Source = BasicTeraData.Instance.ImageDatabase.EntityStats.Source;
        }

        public Dictionary<PlayerInfo, PlayerStats> Controls { get; set; } = new Dictionary<PlayerInfo, PlayerStats>();

        private MenuItem _switchNoStatsVisibility;
        private void TrayConfiguration()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = BasicTeraData.Instance.ImageDatabase.Tray,
                Visible = true,
                Text = "Shinra Meter V" + UpdateManager.Version + ": No server"
            };
            _trayIcon.Click += TrayIconOnClick;
            _trayIcon.DoubleClick += _trayIcon_DoubleClick;

            var reset = new MenuItem {Text = "Reset"};
            reset.Click += ResetOnClick;
            var exit = new MenuItem {Text = "Close"};
            exit.Click += ExitOnClick;
            var wiki = new MenuItem {Text = "Wiki"};
            wiki.Click += WikiOnClick;
            var patch = new MenuItem {Text = "Patch note"};
            patch.Click += PatchOnClick;
            var issues = new MenuItem {Text = "Report issue"};
            issues.Click += IssuesOnClick;
            var forum = new MenuItem {Text = "Forum"};
            forum.Click += ForumOnClick;
            var teradps = new MenuItem { Text = "TeraDps.io" };
            teradps.Click += TeraDpsOnClick;
            var excel = new MenuItem { Text = "Excel export" };
            excel.Click += ExcelOnClick;
            excel.Checked = BasicTeraData.Instance.WindowData.Excel;
            var siteExport = new MenuItem { Text = "Site export" };
            siteExport.Click += SiteOnClick;
            siteExport.Checked = BasicTeraData.Instance.WindowData.SiteExport;
            var party = new MenuItem { Text = "Count only party members" };
            party.Click += PartyOnClick;
            party.Checked = BasicTeraData.Instance.WindowData.PartyOnly;

            _clickThrou = new MenuItem {Text = "Activate click throu"};
            _clickThrou.Click += ClickThrouOnClick;
            _switchNoStatsVisibility = new MenuItem { Text = "Invisible when no stats" };
            _switchNoStatsVisibility.Click += SwitchNoStatsVisibility;
            _switchNoStatsVisibility.Checked = BasicTeraData.Instance.WindowData.InvisibleUI;
            _alwaysOn = new MenuItem { Text = "Show always" };
            _alwaysOn.Click += _trayIcon_DoubleClick;
            _alwaysOn.Checked = BasicTeraData.Instance.WindowData.AlwaysVisible;

            var context = new ContextMenu();
            context.MenuItems.Add(_clickThrou);
            context.MenuItems.Add(_switchNoStatsVisibility);
            context.MenuItems.Add(_alwaysOn);
            context.MenuItems.Add(reset);
            context.MenuItems.Add(wiki);
            context.MenuItems.Add(patch);
            context.MenuItems.Add(issues);
            context.MenuItems.Add(forum);
            context.MenuItems.Add(teradps);
            context.MenuItems.Add(excel);
            context.MenuItems.Add(siteExport);
            context.MenuItems.Add(party);
            context.MenuItems.Add(exit);
            _trayIcon.ContextMenu = context;
        }
        private void SiteOnClick(object sender, EventArgs eventArgs)
        {
            BasicTeraData.Instance.WindowData.SiteExport = !BasicTeraData.Instance.WindowData.SiteExport;
            (sender as MenuItem).Checked = BasicTeraData.Instance.WindowData.SiteExport;
        }
        private void PartyOnClick(object sender, EventArgs eventArgs)
        {
            BasicTeraData.Instance.WindowData.PartyOnly = !BasicTeraData.Instance.WindowData.PartyOnly;
            (sender as MenuItem).Checked = BasicTeraData.Instance.WindowData.PartyOnly;
        }
        private void ExcelOnClick(object sender, EventArgs eventArgs)
        {
            BasicTeraData.Instance.WindowData.Excel = !BasicTeraData.Instance.WindowData.Excel;
            (sender as MenuItem).Checked = BasicTeraData.Instance.WindowData.Excel;
        }

        private void _trayIcon_DoubleClick(object sender, EventArgs e)
        {
            BasicTeraData.Instance.WindowData.AlwaysVisible = !BasicTeraData.Instance.WindowData.AlwaysVisible;
            _alwaysOn.Checked = BasicTeraData.Instance.WindowData.AlwaysVisible;
        }

        private MenuItem _clickThrou;
        private MenuItem _alwaysOn;

        private static void ClickThrouOnClick(object sender, EventArgs eventArgs)
        {
            NetworkController.Instance.SwitchClickThrou();
        }

        private void PatchOnClick(object sender, EventArgs eventArgs)
        {
            Process.Start("explorer.exe", "https://github.com/neowutran/ShinraMeter/wiki/Patch-note");
        }

        private void TeraDpsOnClick(object sender, EventArgs eventArgs)
        {
            Process.Start("explorer.exe", "http://teradps.io");
        }

        private void ExitOnClick(object sender, EventArgs eventArgs)
        {
            VerifyClose();
        }

        private void ResetOnClick(object sender, EventArgs eventArgs)
        {
            NetworkController.Instance.NeedToReset=true;
        }

        private void ForumOnClick(object sender, EventArgs eventArgs)
        {
            Process.Start("explorer.exe",
                "https://discordapp.com/channels/169587841859452928");
        }

        private void IssuesOnClick(object sender, EventArgs eventArgs)
        {
            Process.Start("explorer.exe", "https://github.com/neowutran/ShinraMeter/issues");
        }

        private void WikiOnClick(object sender, EventArgs eventArgs)
        {
            Process.Start("explorer.exe", "https://github.com/neowutran/ShinraMeter/wiki");
        }

        private void SwitchNoStatsVisibility(object sender, EventArgs eventArgs)
        {
           
            var invisibleUi = BasicTeraData.Instance.WindowData.InvisibleUI;
            BasicTeraData.Instance.WindowData.InvisibleUI = !invisibleUi;
            (sender as MenuItem).Checked = BasicTeraData.Instance.WindowData.InvisibleUI;
            if (_forceWindowVisibilityHidden) return;

            if (invisibleUi)
            {
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Controls.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            }

        }

        private void TrayIconOnClick(object sender, EventArgs eventArgs)
        {
            /* //no need
            var e = (MouseEventArgs)eventArgs;
            if (e.Button.ToString() == "Right")
            {
                return;
            }
            */
            StayTopMost();

        }

        public void VerifyClose()
        {
            SetForegroundWindow(new WindowInteropHelper(this).Handle);
            if (MessageBox.Show("Do you want to close the application?", "Close Shinra Meter V" + UpdateManager.Version,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Close();
            }
        }

        public void Exit()
        {
            _trayIcon.Visible = false;
            _trayIcon.Icon = null;
            _trayIcon.Dispose();
            BasicTeraData.Instance.WindowData.Location = new Point(Left, Top);
            NetworkController.Instance.Exit();
        }

        private void ListEncounterOnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            keyEventArgs.Handled = true;
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = default(Exception);
            ex = (Exception) e.ExceptionObject;
            LogError("##### CRASH (version=" + UpdateManager.Version + "): #####\r\n" + ex.Message + "\r\n" +
                      ex.StackTrace + "\r\n" + ex.Source + "\r\n" + ex + "\r\n" + ex.Data + "\r\n" + ex.InnerException +
                      "\r\n" + ex.TargetSite);
            MessageBox.Show("An fatal error has be found, please see the error.log file for more informations.");

        }

        private static void GlobalThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            var ex = default(Exception);
            ex = e.Exception;
            LogError("##### FORM EXCEPTION (version=" + UpdateManager.Version + "): #####\r\n" + ex.Message + "\r\n" +
                      ex.StackTrace + "\r\n" + ex.Source + "\r\n" + ex + "\r\n" + ex.Data + "\r\n" + ex.InnerException +
                      "\r\n" + ex.TargetSite);
            MessageBox.Show("An fatal error has be found, please see the error.log file for more informations.");

        }

        private static void LogError(string error)
        {
            try
            {
                var log = LogManager.GetLogger(typeof(Program));
                log.Error(error);
                if (!BasicTeraData.Instance.WindowData.Debug)
                {
                    return;
                }

                using (var client = new HttpClient())
                {
                   
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("error", error),
                    });

                    var response = client.PostAsync("http://diclah.com/~yukikoo/debug/debug.php", formContent);
                    var responseString = response.Result.Content.ReadAsStringAsync();
                    Console.WriteLine(responseString.Result);
                }
            }
            catch 
            {
             // Ignore
            }
        }

        private bool _forceWindowVisibilityHidden = false;

        public void UpdateKeyboard(object o, EventArgs args)
        {
            var teraWindowActive = TeraWindow.IsTeraActive();
            var meterWindowActive = TeraWindow.IsMeterActive();
            if (!_keyboardInitialized)
            {
                KeyboardHook.Instance.RegisterKeyboardHook();
                _keyboardInitialized = true;
            }
            else
            {
                if (KeyboardHook.Instance.SetHotkeys(teraWindowActive))
                {
                    StayTopMost();
                }
            }

            if (!BasicTeraData.Instance.WindowData.AlwaysVisible)
            {

                if (!teraWindowActive && !meterWindowActive)
                {
                    Visibility = Visibility.Hidden;
                    _forceWindowVisibilityHidden = true;
                }

                if ((meterWindowActive || teraWindowActive) && ((BasicTeraData.Instance.WindowData.InvisibleUI && Controls.Count > 0) || !BasicTeraData.Instance.WindowData.InvisibleUI))
                {
                    _forceWindowVisibilityHidden = false;
                    Visibility = Visibility.Visible;
                }
            }
            else
            {
                _forceWindowVisibilityHidden = false;
            }
        }

        public void Update(long nfirstHit, long nlastHit, long ntotalDamage, long npartyDps, Dictionary<Entity, EntityInfo> nentities,
            List<PlayerInfo> nstats, Entity ncurrentBoss, bool ntimedEncounter, Tera.Game.AbnormalityStorage nabnormals , ConcurrentDictionary<string, Entity> nbossHistory, List<ChatMessage> nchatbox)
        {
            NetworkController.UpdateUiHandler changeUi =
                delegate(long firstHit, long lastHit, long totalDamage, long partyDps, Dictionary<Entity, EntityInfo> entities,
                    List<PlayerInfo> stats, Entity currentBoss, bool timedEncounter, Tera.Game.AbnormalityStorage abnormals, ConcurrentDictionary<string, Entity> bossHistory, List<ChatMessage> chatbox)
                {
                    var entitiesStats = new LinkedList<KeyValuePair<Entity, EntityInfo>>(entities.ToList().OrderByDescending(e => e.Value.LastHit));
                    UpdateComboboxEncounter(entitiesStats, currentBoss);
                    _entityStats.Update(entities, abnormals, currentBoss);
                    _windowHistory.Update(bossHistory);
                    _chatbox.Update(chatbox);
                    PartyDps.Content = FormatHelpers.Instance.FormatValue(partyDps) + "/s";
                    var visiblePlayerStats = new HashSet<PlayerInfo>();
                    var counter = 0;
                    foreach (var playerStats in stats)
                    {
                        PlayerStats playerStatsControl;
                        Controls.TryGetValue(playerStats, out playerStatsControl);
                        if (playerStats.Dealt.Damage(currentBoss, timedEncounter) == 0 && playerStats.Received.Hits(currentBoss, firstHit, lastHit, timedEncounter) == 0)
                        {
                            continue;
                        }
                        visiblePlayerStats.Add(playerStats);
                        if (playerStatsControl != null) continue;
                        playerStatsControl = new PlayerStats(playerStats, abnormals.Get(playerStats.Player));
                        Controls.Add(playerStats, playerStatsControl);

                        if (counter == 9)
                        {
                            break;
                        }
                        counter++;
                    }

                    var invisibleControls = Controls.Where(x => !visiblePlayerStats.Contains(x.Key)).ToList();
                    foreach (var invisibleControl in invisibleControls)
                    {
                        Controls[invisibleControl.Key].CloseSkills();
                        Controls.Remove(invisibleControl.Key);
                    }

                    TotalDamage.Content = FormatHelpers.Instance.FormatValue(totalDamage);
                    var intervalvalue = lastHit - firstHit;
                    var interval = TimeSpan.FromSeconds(intervalvalue);
                    Timer.Content = interval.ToString(@"mm\:ss");

                    Players.Items.Clear();
                    var sortedDict = from entry in Controls
                        orderby
                            stats[stats.IndexOf(entry.Value.PlayerInfo)].Dealt.DamageFraction(currentBoss, totalDamage, timedEncounter) descending
                        select entry;
                    foreach (var item in sortedDict)
                    {
                        Players.Items.Add(item.Value);
                        var data = stats.IndexOf(item.Value.PlayerInfo);
                        
                        item.Value.Repaint(stats[data],abnormals.Get(stats[data].Player), totalDamage, firstHit, lastHit, currentBoss, timedEncounter);
                    }
                       
                    if (BasicTeraData.Instance.WindowData.InvisibleUI)
                    {
                        if(Controls.Count > 0 && !_forceWindowVisibilityHidden)
                        {
                            Visibility = Visibility.Visible;
                        }
                        if(Controls.Count == 0)
                        {
                            Visibility = Visibility.Hidden;
                        }
                    }
                    else
                    {
                        if (!_forceWindowVisibilityHidden)
                        {
                            Visibility = Visibility.Visible;
                        }
                    }
                };
            Dispatcher.Invoke(changeUi, nfirstHit, nlastHit, ntotalDamage, npartyDps, nentities, nstats, ncurrentBoss, ntimedEncounter, nabnormals, nbossHistory, nchatbox);
        }

        private TeradpsHistory _windowHistory;

        private void ShowHistory(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _windowHistory.Show();
        }
        private void ShowChat(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _chatbox.Show();
        }

        public void HandleConnected(string serverName)
        {
            ChangeTitle changeTitle = delegate(string newServerName)
            {
                Title = newServerName;
                _trayIcon.Text = "Shinra Meter V" + UpdateManager.Version + ": " + newServerName;
            };
            Dispatcher.Invoke(changeTitle, serverName);
        }

        private void StayTopMost()
        {
            if (!Topmost || !_topMost) return;
            Topmost = false;
            Topmost = true;
            
        }

        private bool NeedUpdateEncounter(List<KeyValuePair<Entity, EntityInfo>> entities)
        {
            var list = entities.Where(x => (x.Key.IsBoss && x.Value.FirstHit != 0)).ToList();
            if (list.Count != ListEncounter.Items.Count-1)
            {
                return true;
            }
            for (var i = 1; i < ListEncounter.Items.Count - 1; i++)
            {
                if (((Entity)((ComboBoxItem)ListEncounter.Items[i]).Content) != list[i - 1].Key)
                {
                    return true;
                }
            }
            return false;
        }

        private bool ChangeEncounterSelection(Entity entity)
        {
            if (entity == null)
            {
                return false;
            }
            foreach (var item in ListEncounter.Items)
            {
                if (((Entity) ((ComboBoxItem) item).Content) != entity) continue;
                ListEncounter.SelectedItem = item;
                return true;
            }
            return false;
        }

        
        private void UpdateComboboxEncounter(LinkedList<KeyValuePair<Entity, EntityInfo>> entities, Entity currentBoss)
        {

            //http://stackoverflow.com/questions/12164488/system-reflection-targetinvocationexception-occurred-in-presentationframework
            if (ListEncounter == null || !ListEncounter.IsLoaded)
            {
                return;
            }

            var entityList = entities.ToList();
            if (!NeedUpdateEncounter(entityList))
            {
                ChangeEncounterSelection(currentBoss);
                return;
            }

            Entity selectedEntity = null;
            if ((ComboBoxItem) ListEncounter.SelectedItem != null)
            {
                selectedEntity = (Entity) ((ComboBoxItem) ListEncounter.SelectedItem).Content;
            }

            ListEncounter.Items.Clear();
            ListEncounter.Items.Add(new ComboBoxItem {Content = new Entity("TOTAL")});
            var selected = false;
            foreach (var entity in entityList)
            {
                var item = new ComboBoxItem {Content = entity.Key};
                if (!entity.Key.IsBoss ||  entity.Value.FirstHit == 0)
                {
                    continue;
                }
                ListEncounter.Items.Add(item);
                if (entity.Key != selectedEntity) continue;
                ListEncounter.SelectedItem = ListEncounter.Items[ListEncounter.Items.Count - 1];
                selected = true;
            }
            if (ChangeEncounterSelection(currentBoss))
            {
                return;
            }

            if (selected) return;
            ListEncounter.SelectedItem = ListEncounter.Items[0];

        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (BasicTeraData.Instance.WindowData.RememberPosition)
            {
                Top = BasicTeraData.Instance.WindowData.Location.Y;
                Left = BasicTeraData.Instance.WindowData.Location.X;
                return;
            }
            Top = 0;
            Left = 0;
        }

        private void ListEncounter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            var encounter = (Entity) ((ComboBoxItem) e.AddedItems[0]).Content;
            if (encounter.Name.Equals("TOTAL"))
            {
                encounter = null;
            }
            if (encounter != NetworkController.Instance.Encounter)
            {
                NetworkController.Instance.NewEncounter = encounter;
            }
        }

        public void CloseEntityStats()
        {
            _entityStats.Hide();
            _entityStats.Topmost = false;
        }

    
        private delegate void ChangeTitle(string servername);

        private void PinImage_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (Topmost)
            {
                Topmost = false;
                PinImage.Source = BasicTeraData.Instance.ImageDatabase.Pin.Source;
                return;
            }
            Topmost = true;
            PinImage.Source = BasicTeraData.Instance.ImageDatabase.UnPin.Source;
        }

        private void EntityStatsImage_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _entityStats.Show();
            _entityStats.Topmost = false;
            _entityStats.Topmost = true;
        }

        private void Chrono_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (NetworkController.Instance.TimedEncounter)
            {
                NetworkController.Instance.TimedEncounter = false;
                Chrono.Source = BasicTeraData.Instance.ImageDatabase.Chrono.Source;
            }
            else
            {
                NetworkController.Instance.TimedEncounter = true;
                Chrono.Source = BasicTeraData.Instance.ImageDatabase.Chronobar.Source;
            }
        }

        private bool _topMost = true;

        private void ListEncounter_OnDropDownOpened(object sender, EventArgs e)
        {
            _topMost = false;
        }

        private void ListEncounter_OnDropDownClosed(object sender, EventArgs e)
        {
            //.NET 4.6 bug: https://connect.microsoft.com/VisualStudio/feedback/details/1660886/system-windows-controls-combobox-coerceisselectionboxhighlighted-bug
            if (Environment.OSVersion.Version.Major >= 10)
            {
                this.ListEncounter.GetType().GetField("_highlightedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this.ListEncounter, null);
            }
            _topMost = true;
        }

        private void Close_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            VerifyClose();
        }

        private void Config_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Style style = this.FindResource("ShinraContext") as Style;
            popupMenu.Style = style;
            popupMenu.IsOpen = true;
        }
    }
}