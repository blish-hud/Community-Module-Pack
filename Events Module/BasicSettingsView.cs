using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
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
        }

        private void SetPosition_Click(object sender, Blish_HUD.Input.MouseEventArgs e) => EventsModule.ModuleInstance.ShowSetNotificationPositions();

    }
}
