using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;

namespace Events_Module {
    public class BasicSettingsView : View {

        protected override void Build(Container buildPanel) {
            var setPosition = new StandardButton() {
                Text     = "Set Notification Position",
                Width    = 196,
                Location = new Point(32, 32),
                Parent   = buildPanel
            };

            setPosition.Click += SetPosition_Click;

            var notificationToggle = new Checkbox() {
                Text     = "Enable Notifications",
                Checked  = EventsModule.ModuleInstance.NotificationsEnabled,
                Parent   = buildPanel,
                Location = new Point(32, setPosition.Bottom + 20)
            };

            var chimeToggle = new Checkbox {
                Text    = "Mute Notifications",
                Checked = !EventsModule.ModuleInstance.ChimeEnabled,
                Parent  = buildPanel,
                Location = new Point(notificationToggle.Right + 20, notificationToggle.Top)
            };

            var preTimeLabel = new Label() {
                Text = "Pre-Time (min):",
                Parent = buildPanel,
                Location = new Point(32, notificationToggle.Bottom + 20),
                AutoSizeWidth = true
            };

            var preTimeTextBox = new TextBox() {
                Parent = buildPanel,
                Location = new Point(preTimeLabel.Right + 10, preTimeLabel.Top - 5),
                Width = 100,
                Text = EventsModule.ModuleInstance.NotificationPreTime,
                BasicTooltipText = "Comma-separated minutes before event (e.g., 15, 5, 1)"
            };

            var durationLabel = new Label() {
                Text = "Duration (sec):",
                Parent = buildPanel,
                Location = new Point(preTimeTextBox.Right + 20, preTimeLabel.Top),
                AutoSizeWidth = true
            };

            var durationTextBox = new TextBox() {
                Parent = buildPanel,
                Location = new Point(durationLabel.Right + 10, durationLabel.Top - 5),
                Width = 60,
                Text = EventsModule.ModuleInstance.NotificationDuration.ToString(),
                BasicTooltipText = "Show notification for X seconds"
            };

            var showCornerIconToggle = new Checkbox {
                Text    = "Show Corner Icon",
                Checked = EventsModule.ModuleInstance.ShowCornerIcon,
                Parent  = buildPanel,
                Location = new Point(32, preTimeLabel.Bottom + 20)
            };

            notificationToggle.CheckedChanged += delegate (object sender, CheckChangedEvent e) { EventsModule.ModuleInstance.NotificationsEnabled = e.Checked; };
            chimeToggle.CheckedChanged        += delegate (object sender, CheckChangedEvent e) { EventsModule.ModuleInstance.ChimeEnabled         = !e.Checked; };
            showCornerIconToggle.CheckedChanged += delegate (object sender, CheckChangedEvent e) { EventsModule.ModuleInstance.ShowCornerIcon     = e.Checked; };
            preTimeTextBox.TextChanged        += delegate (object sender, System.EventArgs e) { 
                EventsModule.ModuleInstance.NotificationPreTime = preTimeTextBox.Text; 
            };
            durationTextBox.TextChanged += delegate {
                if (int.TryParse(durationTextBox.Text, out int dur) && dur > 0) {
                    EventsModule.ModuleInstance.NotificationDuration = dur;
                }
            };
        }

        private void SetPosition_Click(object sender, Blish_HUD.Input.MouseEventArgs e) => EventsModule.ModuleInstance.ShowSetNotificationPositions();

    }
}
