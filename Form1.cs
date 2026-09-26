using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ExternalIPWidget
{
    public partial class Form1 : Form
    {
        private Label lblInfo;
        private Button btnRefresh;
        private Button btnClose;

        // =========================================
        // CUSTOM TOOLTIP
        // =========================================

        private Panel tooltipPanel;
        private Label lblTooltip;
        private System.Windows.Forms.Timer tooltipHideTimer;

        private DateTime lastUpdate;

        // =========================================
        // HTTP / AUTO REFRESH
        // =========================================

        private readonly HttpClient httpClient =
            new HttpClient();

        private readonly System.Windows.Forms.Timer refreshTimer;

        // =========================================
        // DRAG
        // =========================================

        private bool isDragging;
        private Point dragStartMouse;
        private Point dragStartForm;

        // =========================================
        // TRANSPARENCY
        // =========================================

        private readonly Color transparentColor =
            Color.Magenta;

        // =========================================
        // POSITION FILE
        // =========================================

        private string PositionFile
        {
            get
            {
                string folder =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.ApplicationData),
                        "ExternalIPWidget");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                return Path.Combine(
                    folder,
                    "position.txt");
            }
        }

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public Form1()
        {
            InitializeComponent();

            // =========================================
            // FORM
            // =========================================

            FormBorderStyle =
                FormBorderStyle.None;

            StartPosition =
                FormStartPosition.Manual;

            Width = 216;
            Height = 55;

            ShowInTaskbar = false;
            TopMost = false;

            // Το συγκεκριμένο χρώμα θα είναι transparent.
            BackColor = transparentColor;
            TransparencyKey = transparentColor;

            // =========================================
            // DOUBLE BUFFER
            // =========================================

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            // =========================================
            // ROUNDED CORNERS
            // =========================================

            SetRoundedCorners(14);

            // =========================================
            // LOAD POSITION
            // =========================================

            LoadSavedPosition();

            // =========================================
            // LABEL
            // =========================================

            lblInfo = new Label();

            lblInfo.Dock =
                DockStyle.Fill;

            lblInfo.Text =
                "External IP";

            lblInfo.TextAlign =
                ContentAlignment.MiddleCenter;

            lblInfo.Font =
                new Font(
                    "Segoe UI",
                    11,
                    FontStyle.Bold);

            lblInfo.ForeColor =
                Color.White;

            lblInfo.BackColor =
                Color.Transparent;

            // Αφήνουμε χώρο δεξιά
            // για το refresh button.
            lblInfo.Padding =
                new Padding(
                    0,
                    0,
                    34,
                    0);

            Controls.Add(lblInfo);

            // =========================================
            // LABEL DRAG
            // =========================================

            lblInfo.MouseDown +=
                Widget_MouseDown;

            lblInfo.MouseMove +=
                Widget_MouseMove;

            lblInfo.MouseUp +=
                Widget_MouseUp;

            // =========================================
            // REFRESH BUTTON
            // =========================================

            btnRefresh = new Button();

            btnRefresh.Text =
                "↻";

            btnRefresh.Font =
                new Font(
                    "Segoe UI Symbol",
                    13,
                    FontStyle.Regular);

            btnRefresh.Size =
                new Size(34, 34);

            btnRefresh.FlatStyle =
                FlatStyle.Flat;

            btnRefresh.FlatAppearance.BorderSize =
                0;

            btnRefresh.FlatAppearance.MouseDownBackColor =
                Color.Transparent;

            btnRefresh.FlatAppearance.MouseOverBackColor =
                Color.Transparent;

            btnRefresh.BackColor =
                Color.Transparent;

            btnRefresh.ForeColor =
                Color.White;

            btnRefresh.Cursor =
                Cursors.Hand;

            btnRefresh.TabStop =
                false;

            btnRefresh.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;

            // =========================================
            // REFRESH BUTTON
            // =========================================
            btnRefresh.Location =
                new Point(
                    Width - 65,
                    (Height - btnRefresh.Height) / 2);

            // =========================================
            // REFRESH CLICK
            // =========================================

            btnRefresh.Click +=
                async (s, e) =>
                {
                    await GetIpInfoAsync();
                };

            // =========================================
            // REFRESH HOVER
            // =========================================

            btnRefresh.MouseEnter +=
                (s, e) =>
                {
                    btnRefresh.ForeColor =
                        Color.DeepSkyBlue;

                    btnRefresh.Font =
                        new Font(
                            "Segoe UI Symbol",
                            15,
                            FontStyle.Bold);
                };

            btnRefresh.MouseLeave +=
                (s, e) =>
                {
                    btnRefresh.ForeColor =
                        Color.White;

                    btnRefresh.Font =
                        new Font(
                            "Segoe UI Symbol",
                            13,
                            FontStyle.Regular);
                };

            Controls.Add(btnRefresh);

            // =========================================
            // CLOSE BUTTON
            // =========================================

            btnClose = new Button();

            btnClose.Text = "×";

            btnClose.Font = new Font(
                "Segoe UI",
                13,
                FontStyle.Regular);

            btnClose.Size = new Size(28, 34);

            btnClose.FlatStyle =
                FlatStyle.Flat;

            btnClose.FlatAppearance.BorderSize =
                0;

            btnClose.FlatAppearance.MouseDownBackColor =
                Color.Transparent;

            btnClose.FlatAppearance.MouseOverBackColor =
                Color.Transparent;

            btnClose.BackColor =
                Color.Transparent;

            btnClose.ForeColor =
                Color.White;

            btnClose.Cursor =
                Cursors.Hand;

            btnClose.TabStop =
                false;

            btnClose.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;

            // =========================================
            // CLOSE BUTTON
            // =========================================

            btnClose.Location =
                new Point(
                    Width - 31,
                    (Height - btnClose.Height) / 2);

            btnClose.BringToFront();

            // =========================================
            // CLOSE CLICK
            // =========================================

            btnClose.Click +=
                (s, e) =>
                {
                    Close();
                };

            // =========================================
            // CLOSE HOVER
            // =========================================

            btnClose.MouseEnter +=
                (s, e) =>
                {
                    btnClose.ForeColor =
                        Color.IndianRed;

                    btnClose.Font =
                        new Font(
                            "Segoe UI",
                            14,
                            FontStyle.Bold);
                };

            btnClose.MouseLeave +=
                (s, e) =>
                {
                    btnClose.ForeColor =
                        Color.White;

                    btnClose.Font =
                        new Font(
                            "Segoe UI",
                            13,
                            FontStyle.Regular);
                };

            Controls.Add(btnClose);

            btnClose.BringToFront();

            btnRefresh.BringToFront();

            // =========================================
            // WIDGET MOUSE EVENTS
            // =========================================

            MouseEnter +=
                Widget_MouseEnter;

            MouseLeave +=
                Widget_MouseLeave;

            lblInfo.MouseEnter +=
                Widget_MouseEnter;

            lblInfo.MouseLeave +=
                Widget_MouseLeave;

            btnRefresh.MouseEnter +=
                Widget_MouseEnter;

            btnRefresh.MouseLeave +=
                Widget_MouseLeave;

            btnClose.MouseEnter +=
                Widget_MouseEnter;

            btnClose.MouseLeave +=
                Widget_MouseLeave;

            // =========================================
            // FORM DRAG
            // =========================================

            MouseDown +=
                Widget_MouseDown;

            MouseMove +=
                Widget_MouseMove;

            MouseUp +=
                Widget_MouseUp;

            // =========================================
            // CUSTOM TOOLTIP
            // =========================================

            CreateCustomTooltip();

            // =========================================
            // AUTO REFRESH
            // =========================================

            refreshTimer =
                new System.Windows.Forms.Timer();

            // 5 λεπτά
            refreshTimer.Interval =
                5 * 60 * 1000;

            refreshTimer.Tick +=
                async (s, e) =>
                {
                    await GetIpInfoAsync();
                };

            refreshTimer.Start();
        }

        // =============================================
        // PAINT BACKGROUND
        // =============================================

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            using (SolidBrush brush =
                new SolidBrush(
                    Color.FromArgb(
                        210,
                        28,
                        28,
                        32)))
            {
                e.Graphics.FillRectangle(
                    brush,
                    ClientRectangle);
            }
        }

        // =============================================
        // ROUNDED CORNERS
        // =============================================

        private void SetRoundedCorners(
            int radius)
        {
            IntPtr hRgn =
                CreateRoundRectRgn(
                    0,
                    0,
                    Width + 1,
                    Height + 1,
                    radius,
                    radius);

            Region =
                Region.FromHrgn(hRgn);

            DeleteObject(hRgn);
        }

        protected override void OnSizeChanged(
            EventArgs e)
        {
            base.OnSizeChanged(e);

            SetRoundedCorners(14);
        }

        // =============================================
        // FORM SHOWN
        // =============================================

        protected override async void OnShown(
            EventArgs e)
        {
            base.OnShown(e);

            if (!HasSavedPosition())
            {
                Rectangle workingArea =
                    Screen.PrimaryScreen.WorkingArea;

                Left =
                    (workingArea.Width - Width) / 2;

                Top =
                    workingArea.Bottom -
                    Height -
                    20;
            }

            await GetIpInfoAsync();
        }

        // =============================================
        // GET IP
        // =============================================

        private async Task GetIpInfoAsync()
        {
            try
            {
                btnRefresh.Enabled =
                    false;

                string json =
                    await httpClient.GetStringAsync(
                        "https://ipwho.is/");

                using (JsonDocument document =
                    JsonDocument.Parse(json))
                {
                    JsonElement root =
                        document.RootElement;

                    bool success =
                        root.GetProperty("success")
                            .GetBoolean();

                    if (!success)
                    {
                        lblInfo.Text =
                            "IP Error";

                        return;
                    }

                    string ip =
                        root.GetProperty("ip")
                            .GetString();

                    string country =
                        root.GetProperty("country")
                            .GetString();

                    lblInfo.Text =
                        ip +
                        Environment.NewLine +
                        country;

                    // Αποθηκεύουμε την ώρα
                    // της τελευταίας επιτυχημένης ενημέρωσης.
                    lastUpdate =
                        DateTime.Now;

                    UpdateTooltipText();
                }
            }
            catch (Exception ex)
            {
                lblInfo.Text =
                    "IP Error";

                MessageBox.Show(
                    "Δεν ήταν δυνατή η λήψη της External IP.\n\n" +
                    ex.Message,
                    "External IP Widget",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled =
                    true;
            }
        }

        // =============================================
        // DRAG START
        // =============================================

        private void Widget_MouseDown(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button !=
                MouseButtons.Left)
            {
                return;
            }

            isDragging =
                true;

            dragStartMouse =
                Cursor.Position;

            dragStartForm =
                Location;
        }

        // =============================================
        // DRAG MOVE
        // =============================================

        private void Widget_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!isDragging)
                return;

            Point currentMouse =
                Cursor.Position;

            int deltaX =
                currentMouse.X -
                dragStartMouse.X;

            int deltaY =
                currentMouse.Y -
                dragStartMouse.Y;

            Location =
                new Point(
                    dragStartForm.X + deltaX,
                    dragStartForm.Y + deltaY);
        }

        // =============================================
        // DRAG END
        // =============================================

        private void Widget_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button !=
                MouseButtons.Left)
            {
                return;
            }

            if (!isDragging)
                return;

            isDragging =
                false;

            SavePosition();
        }

        // =============================================
        // CREATE CUSTOM TOOLTIP
        // =============================================

        private void CreateCustomTooltip()
        {
            tooltipPanel =
                new Panel();

            tooltipPanel.Size =
                new Size(
                    125,
                    28);

            tooltipPanel.BackColor =
                Color.FromArgb(
                    35,
                    35,
                    40);

            tooltipPanel.Visible =
                false;

            tooltipPanel.Cursor =
                Cursors.Default;

            tooltipPanel.Region =
                CreateRoundedRegion(
                    tooltipPanel.Width,
                    tooltipPanel.Height,
                    10);

            lblTooltip =
                new Label();

            lblTooltip.Dock =
                DockStyle.Fill;

            lblTooltip.TextAlign =
                ContentAlignment.MiddleCenter;

            lblTooltip.Font =
                new Font(
                    "Segoe UI",
                    8.5f,
                    FontStyle.Regular);

            lblTooltip.ForeColor =
                Color.FromArgb(
                    225,
                    225,
                    230);

            lblTooltip.BackColor =
                Color.Transparent;

            lblTooltip.Cursor =
                Cursors.Default;

            tooltipPanel.Controls.Add(
                lblTooltip);

            Controls.Add(
                tooltipPanel);

            tooltipPanel.BringToFront();

            // =========================================
            // TOOLTIP TIMER
            // =========================================

            tooltipHideTimer =
                new System.Windows.Forms.Timer();

            // Μικρή καθυστέρηση ώστε να μην
            // εξαφανίζεται όταν περνάμε
            // από το widget στο tooltip.
            tooltipHideTimer.Interval =
                150;

            tooltipHideTimer.Tick +=
                TooltipHideTimer_Tick;

            // =========================================
            // TOOLTIP EVENTS
            // =========================================

            tooltipPanel.MouseEnter +=
                Tooltip_MouseEnter;

            tooltipPanel.MouseLeave +=
                Tooltip_MouseLeave;

            lblTooltip.MouseEnter +=
                Tooltip_MouseEnter;

            lblTooltip.MouseLeave +=
                Tooltip_MouseLeave;
        }

        // =============================================
        // UPDATE TOOLTIP TEXT
        // =============================================

        private void UpdateTooltipText()
        {
            if (lblTooltip == null)
                return;

            if (lastUpdate ==
                DateTime.MinValue)
            {
                return;
            }

            lblTooltip.Text =
                "Last update: " +
                lastUpdate.ToString("HH:mm");
        }

        // =============================================
        // SHOW CUSTOM TOOLTIP
        // =============================================

        private void ShowCustomTooltip()
        {
            if (lastUpdate ==
                DateTime.MinValue)
            {
                return;
            }

            tooltipHideTimer.Stop();

            UpdateTooltipText();

            Point mousePosition =
                PointToClient(
                    Cursor.Position);

            int x =
                mousePosition.X +
                12;

            int y =
                mousePosition.Y +
                18;

            // =========================================
            // RIGHT EDGE
            // =========================================

            if (x +
                tooltipPanel.Width >
                Width)
            {
                x =
                    mousePosition.X -
                    tooltipPanel.Width -
                    12;
            }

            // =========================================
            // BOTTOM EDGE
            // =========================================

            if (y +
                tooltipPanel.Height >
                Height)
            {
                y =
                    mousePosition.Y -
                    tooltipPanel.Height -
                    12;
            }

            // =========================================
            // TOP EDGE
            // =========================================

            if (y < 0)
            {
                y =
                    mousePosition.Y +
                    18;
            }

            // =========================================
            // LEFT EDGE
            // =========================================

            if (x < 0)
            {
                x = 5;
            }

            tooltipPanel.Location =
                new Point(
                    x,
                    y);

            tooltipPanel.Visible =
                true;

            tooltipPanel.BringToFront();
        }

        // =============================================
        // START HIDE TIMER
        // =============================================

        private void StartTooltipHideTimer()
        {
            tooltipHideTimer.Stop();
            tooltipHideTimer.Start();
        }

        // =============================================
        // TOOLTIP HIDE TIMER
        // =============================================

        private void TooltipHideTimer_Tick(
            object sender,
            EventArgs e)
        {
            tooltipHideTimer.Stop();

            Point mousePosition =
                PointToClient(
                    Cursor.Position);

            Rectangle widgetArea =
                ClientRectangle;

            Rectangle tooltipArea =
                tooltipPanel.Bounds;

            // Ο cursor βρίσκεται ακόμα
            // πάνω στο widget ή tooltip.
            if (widgetArea.Contains(
                    mousePosition) ||
                tooltipArea.Contains(
                    mousePosition))
            {
                return;
            }

            tooltipPanel.Visible =
                false;
        }

        // =============================================
        // WIDGET ENTER
        // =============================================

        private void Widget_MouseEnter(
            object sender,
            EventArgs e)
        {
            tooltipHideTimer.Stop();

            ShowCustomTooltip();
        }

        // =============================================
        // WIDGET LEAVE
        // =============================================

        private void Widget_MouseLeave(
            object sender,
            EventArgs e)
        {
            StartTooltipHideTimer();
        }

        // =============================================
        // TOOLTIP ENTER
        // =============================================

        private void Tooltip_MouseEnter(
            object sender,
            EventArgs e)
        {
            tooltipHideTimer.Stop();
        }

        // =============================================
        // TOOLTIP LEAVE
        // =============================================

        private void Tooltip_MouseLeave(
            object sender,
            EventArgs e)
        {
            StartTooltipHideTimer();
        }

        // =============================================
        // LOAD POSITION
        // =============================================

        private void LoadSavedPosition()
        {
            try
            {
                if (!File.Exists(
                    PositionFile))
                {
                    return;
                }

                string[] lines =
                    File.ReadAllLines(
                        PositionFile);

                if (lines.Length < 2)
                    return;

                int x;
                int y;

                if (!int.TryParse(
                    lines[0],
                    out x))
                {
                    return;
                }

                if (!int.TryParse(
                    lines[1],
                    out y))
                {
                    return;
                }

                Rectangle area =
                    SystemInformation.VirtualScreen;

                // =========================================
                // LEFT
                // =========================================

                if (x < area.Left)
                {
                    x =
                        area.Left + 10;
                }

                // =========================================
                // TOP
                // =========================================

                if (y < area.Top)
                {
                    y =
                        area.Top + 10;
                }

                // =========================================
                // RIGHT
                // =========================================

                if (x >
                    area.Right -
                    Width)
                {
                    x =
                        area.Right -
                        Width -
                        10;
                }

                // =========================================
                // BOTTOM
                // =========================================

                if (y >
                    area.Bottom -
                    Height)
                {
                    y =
                        area.Bottom -
                        Height -
                        10;
                }

                Location =
                    new Point(
                        x,
                        y);
            }
            catch
            {
                // Default position
            }
        }

        // =============================================
        // HAS SAVED POSITION
        // =============================================

        private bool HasSavedPosition()
        {
            try
            {
                return File.Exists(
                    PositionFile);
            }
            catch
            {
                return false;
            }
        }

        // =============================================
        // SAVE POSITION
        // =============================================

        private void SavePosition()
        {
            try
            {
                File.WriteAllLines(
                    PositionFile,
                    new string[]
                    {
                        Location.X.ToString(),
                        Location.Y.ToString()
                    });
            }
            catch
            {
                // Ignore save errors
            }
        }

        // =============================================
        // FORM CLOSE
        // =============================================

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            SavePosition();

            if (refreshTimer != null)
            {
                refreshTimer.Stop();
            }

            if (tooltipHideTimer != null)
            {
                tooltipHideTimer.Stop();
                tooltipHideTimer.Dispose();
            }

            base.OnFormClosed(e);
        }

        // =============================================
        // CREATE ROUNDED REGION
        // =============================================

        private Region CreateRoundedRegion(
            int width,
            int height,
            int radius)
        {
            IntPtr hRgn =
                CreateRoundRectRgn(
                    0,
                    0,
                    width,
                    height,
                    radius,
                    radius);

            Region region =
                Region.FromHrgn(
                    hRgn);

            DeleteObject(hRgn);

            return region;
        }

        // =============================================
        // WINDOWS API
        // =============================================

        [DllImport(
            "gdi32.dll",
            EntryPoint =
                "CreateRoundRectRgn")]
        private static extern IntPtr
            CreateRoundRectRgn(
                int nLeftRect,
                int nTopRect,
                int nRightRect,
                int nBottomRect,
                int nWidthEllipse,
                int nHeightEllipse);

        [DllImport(
            "gdi32.dll",
            EntryPoint =
                "DeleteObject")]
        private static extern bool
            DeleteObject(
                IntPtr hObject);
    }
}

// Το αρχείο θέσης θα βρίσκεται εδώ: C:\Users\<το όνομά σου>\AppData\Roaming\ExternalIPWidget\position.txt