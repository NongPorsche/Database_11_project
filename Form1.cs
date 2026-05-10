// ============================================================
//  Event Management System — Form1.cs
//  ภาษา: C#  |  UI: WinForms  |  DB: MySQL
//
//  NuGet ที่ต้องติดตั้ง:
//    - MySql.Data
//    - BCrypt.Net-Next
// ============================================================
using BCrypt.Net;   // [เพิ่ม] สำหรับ verify password ที่ hash ไว้
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace event_management_app
{
    public partial class Form1 : Form
    {
        // ==========================================
        // ตัวแปรส่วนกลาง (Global Variables)
        // ==========================================
        Panel pnlWelcome = new Panel();
        Panel pnlRole = new Panel();
        Panel pnlClient = new Panel();
        Panel pnlLogin = new Panel();
        Panel pnlAdmin = new Panel();

        Panel pnlAdminOverview = new Panel();
        Panel pnlAdminStaff = new Panel();
        Panel pnlAdminVendors = new Panel();
        Panel pnlAdminSettings = new Panel();
        Panel pnlAdminBudget = new Panel();   // [เพิ่ม] หน้าสรุปงบประมาณ
        Panel pnlAdminAssignments = new Panel();   // [เพิ่ม] หน้า assign พนักงาน

        DataGridView dgvPending;
        Label lblAdminHeaderTitle = new Label();

        Color bgBase = Color.FromArgb(30, 30, 46);
        Color bgCard = Color.FromArgb(39, 41, 61);
        Color bgSidebar = Color.FromArgb(20, 20, 32);
        Color textWhite = Color.White;
        Color textMuted = Color.FromArgb(140, 140, 160);
        Color accentBlue = Color.FromArgb(52, 114, 247);
        Color successGreen = Color.FromArgb(40, 167, 69);
        Color dangerRed = Color.FromArgb(220, 53, 69);
        Color inputBg = Color.FromArgb(55, 55, 75);
        Color gridLineColor = Color.FromArgb(55, 55, 70);

        // ⚠️ แก้รหัสผ่าน Database ให้ตรงกับเครื่องคุณ
        string connectionString = "server=localhost;user=root;database=event_management;port=3306;password=******;";

        public Form1()
        {
            InitializeComponent();
            this.Text = "Event Management System";
            this.WindowState = FormWindowState.Maximized;
            InitializeCustomUI();

        }

        // ==========================================
        // INIT UI
        // ==========================================
        private void InitializeCustomUI()
        {
            Panel[] mainPanels = { pnlWelcome, pnlRole, pnlClient, pnlLogin, pnlAdmin };
            foreach (var pnl in mainPanels)
            {
                pnl.Dock = DockStyle.Fill;
                pnl.BackColor = bgBase;
                pnl.Visible = false;
                this.Controls.Add(pnl);
            }

            Action<Panel> ShowMainScreen = (targetPanel) => {
                foreach (var pnl in mainPanels) pnl.Visible = false;
                targetPanel.Visible = true;
                targetPanel.BringToFront();
            };

            SetupWelcomeScreen(ShowMainScreen);
            SetupRoleScreen(ShowMainScreen);
            SetupClientScreen(ShowMainScreen);
            SetupLoginScreen(ShowMainScreen);

            // ==========================================
            // Admin Dashboard Header & Sidebar
            // ==========================================
            Panel adminHeader = new Panel() { Height = 60, Dock = DockStyle.Top, BackColor = bgCard };
            pnlAdmin.Controls.Add(adminHeader);

            Button btnToggleMenu = new Button()
            {
                Text = "☰",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Left,
                Width = 60,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnToggleMenu.FlatAppearance.BorderSize = 0;
            adminHeader.Controls.Add(btnToggleMenu);

            lblAdminHeaderTitle.Text = "Admin Workspace";
            lblAdminHeaderTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblAdminHeaderTitle.ForeColor = textWhite;
            lblAdminHeaderTitle.Location = new Point(70, 15);
            lblAdminHeaderTitle.AutoSize = true;
            adminHeader.Controls.Add(lblAdminHeaderTitle);

            Panel adminSidebar = new Panel() { Width = 280, BackColor = bgSidebar, Visible = false };
            pnlAdmin.Controls.Add(adminSidebar);

            pnlAdmin.Resize += (s, e) => {
                adminSidebar.Location = new Point(0, adminHeader.Height);
                adminSidebar.Height = pnlAdmin.Height - adminHeader.Height;
            };

            btnToggleMenu.Click += (s, e) => {
                adminSidebar.Visible = !adminSidebar.Visible;
                if (adminSidebar.Visible) adminSidebar.BringToFront();
            };

            Panel adminContent = new Panel() { Dock = DockStyle.Fill, BackColor = bgBase };
            pnlAdmin.Controls.Add(adminContent);
            adminHeader.SendToBack(); adminContent.BringToFront();

            // [เพิ่ม] ใส่ pnlAdminBudget และ pnlAdminAssignments เข้า adminSubPanels ด้วย
            Panel[] adminSubPanels = {
                pnlAdminOverview, pnlAdminStaff, pnlAdminVendors,
                pnlAdminSettings, pnlAdminBudget, pnlAdminAssignments
            };
            foreach (var subPnl in adminSubPanels)
            {
                subPnl.Dock = DockStyle.Fill;
                subPnl.BackColor = bgBase;
                subPnl.Visible = false;
                adminContent.Controls.Add(subPnl);
            }

            Action<Panel> ShowAdminMenu = (targetSubPanel) => {
                foreach (var subPnl in adminSubPanels) subPnl.Visible = false;
                targetSubPanel.Visible = true;
                targetSubPanel.BringToFront();
                adminSidebar.Visible = false;
            };

            // ปุ่ม sidebar เดิม + ปุ่มใหม่
            Button btnMenuOverview = CreateAdminMenuButton("📊 Overview & Approvals", 20);
            Button btnMenuStaff = CreateAdminMenuButton("👥 Staff & Assignments", 80);
            Button btnMenuVendors = CreateAdminMenuButton("🏢 Vendors & Expenses", 140);
            Button btnMenuBudget = CreateAdminMenuButton("💰 Budget Summary", 200); // [เพิ่ม]
            Button btnMenuAssignments = CreateAdminMenuButton("📋 Assignments", 260); // [เพิ่ม]
            Button btnMenuSettings = CreateAdminMenuButton("⚙️ System Settings", 320);

            adminSidebar.Controls.Add(btnMenuOverview);
            adminSidebar.Controls.Add(btnMenuStaff);
            adminSidebar.Controls.Add(btnMenuVendors);
            adminSidebar.Controls.Add(btnMenuBudget);
            adminSidebar.Controls.Add(btnMenuAssignments);
            adminSidebar.Controls.Add(btnMenuSettings);

            Button btnLogout = new Button()
            {
                Text = "⬅ LOGOUT",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = dangerRed,
                BackColor = bgSidebar,
                Dock = DockStyle.Bottom,
                Height = 60,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            adminSidebar.Controls.Add(btnLogout);

            btnMenuOverview.Click += (s, e) => ShowAdminMenu(pnlAdminOverview);
            btnMenuStaff.Click += (s, e) => ShowAdminMenu(pnlAdminStaff);
            btnMenuVendors.Click += (s, e) => ShowAdminMenu(pnlAdminVendors);
            btnMenuSettings.Click += (s, e) => ShowAdminMenu(pnlAdminSettings);
            // [เพิ่ม] เชื่อมปุ่มใหม่กับหน้าใหม่ พร้อมโหลดข้อมูลทันทีเมื่อคลิก
            btnMenuBudget.Click += (s, e) => { ShowAdminMenu(pnlAdminBudget); LoadBudgetSummary(); };
            btnMenuAssignments.Click += (s, e) => { ShowAdminMenu(pnlAdminAssignments); LoadAssignmentsData(); };

            btnLogout.Click += (s, e) => {
                lblAdminHeaderTitle.Text = "Admin Workspace";
                ShowMainScreen(pnlWelcome);
            };

            // ==========================================
            // 5.1 Overview
            // ==========================================
            pnlAdminOverview.Controls.Clear();
            Label lblOverviewTitle = new Label()
            {
                Text = "Event Requests Management",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(40, 30),
                AutoSize = true
            };
            pnlAdminOverview.Controls.Add(lblOverviewTitle);

            Label lblFilter = new Label()
            {
                Text = "Filter by Status:",
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 12),
                Location = new Point(40, 95),
                AutoSize = true
            };
            pnlAdminOverview.Controls.Add(lblFilter);

            ComboBox cmbFilterStatus = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12),
                Location = new Point(185, 95),
                Width = 200,
                BackColor = inputBg,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat
            };
            cmbFilterStatus.Items.AddRange(new string[] { "All", "Pitching", "Preparing", "Ongoing", "Completed", "Cancelled" });
            cmbFilterStatus.SelectedIndex = 0;
            pnlAdminOverview.Controls.Add(cmbFilterStatus);

            Button btnRefresh = new Button()
            {
                Text = "🔄 Refresh Data",
                Size = new Size(180, 40),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            pnlAdminOverview.Controls.Add(btnRefresh);

            dgvPending = CreateStyledDataGrid();
            dgvPending.Location = new Point(40, 150);
            pnlAdminOverview.Controls.Add(dgvPending);

            Label lblActionTitle = new Label()
            {
                Text = "Change Status For Selected Event:",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentBlue,
                AutoSize = true
            };
            pnlAdminOverview.Controls.Add(lblActionTitle);

            FlowLayoutPanel flpStatusButtons = new FlowLayoutPanel() { Width = 900, Height = 60 };
            pnlAdminOverview.Controls.Add(flpStatusButtons);

            Button CreateStatusBtn(string text, Color bg, string statusValue)
            {
                Button btn = new Button()
                {
                    Text = text,
                    Size = new Size(150, 45),
                    BackColor = bg,
                    ForeColor = textWhite,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 10, 0)
                };
                btn.FlatAppearance.BorderSize = 0;
                // [แก้] เรียก SP แทน UPDATE ตรงๆ
                btn.Click += (s, e) => UpdateEventStatusViaSP(statusValue, cmbFilterStatus.SelectedItem.ToString());
                return btn;
            }

            flpStatusButtons.Controls.Add(CreateStatusBtn("📌 Pitching", Color.FromArgb(108, 117, 125), "Pitching"));
            flpStatusButtons.Controls.Add(CreateStatusBtn("⏳ Preparing", accentBlue, "Preparing"));
            flpStatusButtons.Controls.Add(CreateStatusBtn("🚀 Ongoing", Color.FromArgb(23, 162, 184), "Ongoing"));
            flpStatusButtons.Controls.Add(CreateStatusBtn("✅ Completed", successGreen, "Completed"));
            flpStatusButtons.Controls.Add(CreateStatusBtn("❌ Cancelled", dangerRed, "Cancelled"));

            cmbFilterStatus.SelectedIndexChanged += (s, e) => LoadEventsData(cmbFilterStatus.SelectedItem.ToString());
            btnRefresh.Click += (s, e) => LoadEventsData(cmbFilterStatus.SelectedItem.ToString());

            pnlAdminOverview.Resize += (s, e) => {
                btnRefresh.Location = new Point(pnlAdminOverview.Width - 220, 90);
                dgvPending.Width = pnlAdminOverview.Width - 80;
                dgvPending.Height = pnlAdminOverview.Height - 320;
                lblActionTitle.Location = new Point(40, dgvPending.Bottom + 20);
                flpStatusButtons.Location = new Point(40, lblActionTitle.Bottom + 10);
            };

            SetupAdminStaffAndVendors();
            SetupBudgetSummaryPage();      // [เพิ่ม]
            SetupAssignmentsPage();        // [เพิ่ม]
            SetupSystemSettings(ShowMainScreen);

            ShowMainScreen(pnlWelcome);
        }

        // ==========================================
        // [เพิ่ม] หน้า Budget Summary
        // ดึงข้อมูลจาก View v_event_budget_summary
        // ==========================================
        private void SetupBudgetSummaryPage()
        {
            pnlAdminBudget.Controls.Clear();
            pnlAdminBudget.Controls.Add(new Label()
            {
                Text = "Budget Summary",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(40, 30),
                AutoSize = true
            });

            // คำอธิบายว่า View คืออะไร
            pnlAdminBudget.Controls.Add(new Label()
            {
                Text = "ข้อมูลดึงจาก View: v_event_budget_summary",
                Font = new Font("Segoe UI", 10),
                ForeColor = textMuted,
                Location = new Point(40, 75),
                AutoSize = true
            });

            DataGridView dgvBudget = CreateStyledDataGrid();
            dgvBudget.Location = new Point(40, 100);
            dgvBudget.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Event ID", FillWeight = 8 });
            dgvBudget.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Event Name", FillWeight = 28 });
            dgvBudget.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Client", FillWeight = 20 });
            dgvBudget.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Status", FillWeight = 12 });
            dgvBudget.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Budget (฿)", FillWeight = 16 });
            dgvBudget.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Spent (฿)", FillWeight = 16 });
            dgvBudget.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Remaining (฿)", FillWeight = 16 });
            pnlAdminBudget.Controls.Add(dgvBudget);

            Button btnRefreshBudget = new Button()
            {
                Text = "🔄 Refresh",
                Size = new Size(150, 40),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefreshBudget.FlatAppearance.BorderSize = 0;
            pnlAdminBudget.Controls.Add(btnRefreshBudget);
            btnRefreshBudget.Click += (s, e) => LoadBudgetSummary();

            pnlAdminBudget.Resize += (s, e) => {
                btnRefreshBudget.Location = new Point(pnlAdminBudget.Width - 190, 68);
                dgvBudget.Width = pnlAdminBudget.Width - 80;
                dgvBudget.Height = pnlAdminBudget.Height - 150;
            };

            // เก็บ reference dgvBudget ไว้ใช้ตอน LoadBudgetSummary
            dgvBudget.Name = "dgvBudget";
        }

        private void LoadBudgetSummary()
        {
            if (pnlAdminBudget.Controls.Find("dgvBudget", false).Length == 0) return;
            var dgv = (DataGridView)pnlAdminBudget.Controls.Find("dgvBudget", false)[0];
            dgv.Rows.Clear();

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // [เพิ่ม] เรียกใช้ View v_event_budget_summary ที่สร้างไว้ใน 02_views.sql
                    string sql = "SELECT event_id, event_name, company_name, status, " +
                                 "budget, total_spent, budget_remaining " +
                                 "FROM v_event_budget_summary ORDER BY event_id DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            decimal remaining = reader.IsDBNull(reader.GetOrdinal("budget_remaining"))
                                                ? 0 : Convert.ToDecimal(reader["budget_remaining"]);

                            int rowIdx = dgv.Rows.Add(
                                reader["event_id"],
                                reader["event_name"],
                                reader["company_name"],
                                reader["status"],
                                Convert.ToDecimal(reader["budget"]).ToString("N2"),
                                Convert.ToDecimal(reader["total_spent"]).ToString("N2"),
                                remaining.ToString("N2")
                            );

                            // ไฮไลต์แถวที่งบเกิน (remaining < 0) ด้วยสีแดง
                            if (remaining < 0)
                                dgv.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(255, 100, 100);
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Budget Error: " + ex.Message); }
            }
        }

        // ==========================================
        // [เพิ่ม] หน้า Assignments
        // assign พนักงานลงงาน + เรียก SP sp_assign_employee
        // ==========================================
        private void SetupAssignmentsPage()
        {
            pnlAdminAssignments.Controls.Clear();
            pnlAdminAssignments.AutoScroll = true;

            pnlAdminAssignments.Controls.Add(new Label()
            {
                Text = "Event Assignments",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(40, 30),
                AutoSize = true
            });
            pnlAdminAssignments.Controls.Add(new Label()
            {
                Text = "เรียก Stored Procedure: sp_assign_employee",
                Font = new Font("Segoe UI", 10),
                ForeColor = textMuted,
                Location = new Point(40, 75),
                AutoSize = true
            });

            // ตารางแสดง assignment ทั้งหมด
            DataGridView dgvAssign = CreateStyledDataGrid();
            dgvAssign.Name = "dgvAssign";
            dgvAssign.Location = new Point(40, 100);
            dgvAssign.Height = 250;
            dgvAssign.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvAssign.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Assignment ID", FillWeight = 12 });
            dgvAssign.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Event Name", FillWeight = 30 });
            dgvAssign.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Employee", FillWeight = 28 });
            dgvAssign.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Task Role", FillWeight = 20 });
            dgvAssign.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Assigned", FillWeight = 16 });
            pnlAdminAssignments.Controls.Add(dgvAssign);

            // ฟอร์ม assign ใหม่
            Panel assignForm = new Panel()
            {
                Location = new Point(40, 370),
                Height = 200,
                BackColor = bgCard,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlAdminAssignments.Controls.Add(assignForm);

            assignForm.Controls.Add(new Label()
            {
                Text = "➕  Assign Employee to Event",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = accentBlue,
                Location = new Point(15, 12),
                AutoSize = true
            });

            TableLayoutPanel tlp = new TableLayoutPanel()
            {
                Location = new Point(10, 45),
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            assignForm.Controls.Add(tlp);

            ComboBox cmbEvent = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList };
            ComboBox cmbEmployee = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList };
            TextBox txtTaskRole = new TextBox();

            AddCustomInput(tlp, "Select Event", cmbEvent, 0, 0);
            AddCustomInput(tlp, "Select Employee", cmbEmployee, 1, 0);
            AddCustomInput(tlp, "Task Role", txtTaskRole, 2, 0);

            // โหลด dropdown events และ employees จาก DB
            void LoadDropdowns()
            {
                cmbEvent.Items.Clear();
                cmbEmployee.Items.Clear();
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        // [เพิ่ม] ใช้ View v_active_events กรอง is_deleted อัตโนมัติ
                        using (var cmd = new MySqlCommand(
                            "SELECT event_id, event_name FROM v_active_events ORDER BY event_id DESC", conn))
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                cmbEvent.Items.Add($"{r["event_id"]} — {r["event_name"]}");

                        using (var cmd2 = new MySqlCommand(
                            "SELECT employee_id, CONCAT(first_name,' ',last_name) AS name " +
                            "FROM employees WHERE is_deleted=0 ORDER BY first_name", conn))
                        using (var r2 = cmd2.ExecuteReader())
                            while (r2.Read())
                                cmbEmployee.Items.Add($"{r2["employee_id"]} — {r2["name"]}");
                    }
                    catch { }
                }
            }
            LoadDropdowns();

            Button btnAssign = new Button()
            {
                Text = "Assign",
                Size = new Size(130, 45),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAssign.FlatAppearance.BorderSize = 0;
            assignForm.Controls.Add(btnAssign);

            pnlAdminAssignments.Resize += (s, e) => {
                dgvAssign.Width = pnlAdminAssignments.Width - 80;
                assignForm.Width = pnlAdminAssignments.Width - 80;
                tlp.Width = assignForm.Width - 20;
                btnAssign.Location = new Point(assignForm.Width - btnAssign.Width - 20,
                                               assignForm.Height - btnAssign.Height - 15);
            };

            btnAssign.Click += (s, e) => {
                if (cmbEvent.SelectedItem == null || cmbEmployee.SelectedItem == null)
                {
                    MessageBox.Show("กรุณาเลือก Event และ Employee", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // แยก ID ออกจาก "1 — ชื่อ"
                int eventId = int.Parse(cmbEvent.SelectedItem.ToString().Split('—')[0].Trim());
                int employeeId = int.Parse(cmbEmployee.SelectedItem.ToString().Split('—')[0].Trim());
                string role = txtTaskRole.Text.Trim();

                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        // [เพิ่ม] เรียก Stored Procedure sp_assign_employee
                        using (var cmd = new MySqlCommand("sp_assign_employee", conn))
                        {
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_event_id", eventId);
                            cmd.Parameters.AddWithValue("p_employee_id", employeeId);
                            cmd.Parameters.AddWithValue("p_task_role", role);
                            var outMsg = cmd.Parameters.Add("p_message", MySqlDbType.VarChar, 255);
                            outMsg.Direction = System.Data.ParameterDirection.Output;
                            cmd.ExecuteNonQuery();

                            string msg = outMsg.Value?.ToString() ?? "";
                            if (msg.StartsWith("SUCCESS"))
                            {
                                MessageBox.Show(msg, "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                txtTaskRole.Clear();
                                cmbEvent.SelectedIndex = -1;
                                cmbEmployee.SelectedIndex = -1;
                                LoadAssignmentsData();
                            }
                            else
                                MessageBox.Show(msg, "ไม่สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
                }
            };
        }

        private void LoadAssignmentsData()
        {
            if (pnlAdminAssignments.Controls.Find("dgvAssign", false).Length == 0) return;
            var dgv = (DataGridView)pnlAdminAssignments.Controls.Find("dgvAssign", false)[0];
            dgv.Rows.Clear();

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string sql = @"SELECT ea.assignment_id,
                                          e.event_name,
                                          CONCAT(emp.first_name,' ',emp.last_name) AS employee_name,
                                          ea.task_role,
                                          DATE_FORMAT(ea.assigned_at,'%d/%m/%Y') AS assigned_date
                                   FROM event_assignments ea
                                   JOIN events    e   ON ea.event_id    = e.event_id
                                   JOIN employees emp ON ea.employee_id = emp.employee_id
                                   WHERE e.is_deleted  = 0
                                     AND emp.is_deleted = 0
                                   ORDER BY ea.assignment_id DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            dgv.Rows.Add(reader["assignment_id"], reader["event_name"],
                                         reader["employee_name"], reader["task_role"],
                                         reader["assigned_date"]);
                }
                catch (Exception ex) { MessageBox.Show("Assignment Error: " + ex.Message); }
            }
        }

        // ==========================================
        // Database Load Functions
        // ==========================================
        private void LoadEventsData(string statusFilter = "All")
        {
            dgvPending.Columns.Clear();
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Event ID", FillWeight = 10 });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Client Name", FillWeight = 20 });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Event Name", FillWeight = 25 });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Start Date", FillWeight = 15 });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Budget", FillWeight = 15 });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Status", FillWeight = 15 });
            dgvPending.Rows.Clear();

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // [แก้] ใช้ View v_active_events แทน query ตรงๆ
                    // ทำให้ WHERE is_deleted=0 ถูกจัดการใน View อัตโนมัติ
                    string sql = @"SELECT event_id, company_name, event_name,
                                          DATE_FORMAT(start_date,'%d/%m/%Y') AS s_date,
                                          budget, status
                                   FROM v_active_events";

                    if (statusFilter != "All") sql += " WHERE status = @status";
                    sql += " ORDER BY event_id DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        if (statusFilter != "All") cmd.Parameters.AddWithValue("@status", statusFilter);
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                                dgvPending.Rows.Add(
                                    reader["event_id"], reader["company_name"], reader["event_name"],
                                    reader["s_date"],
                                    Convert.ToDecimal(reader["budget"]).ToString("N2"),
                                    reader["status"]
                                );
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void LoadStaffData(DataGridView dgv)
        {
            dgv.Rows.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // [แก้] เพิ่ม WHERE is_deleted=0 กรองพนักงานที่ถูก soft delete ออก
                    using (var cmd = new MySqlCommand(
                        "SELECT employee_id, first_name, last_name, department, role " +
                        "FROM employees WHERE is_deleted = 0", conn))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            dgv.Rows.Add(reader["employee_id"], reader["first_name"],
                                         reader["last_name"], reader["department"], reader["role"]);
                }
                catch { }
            }
        }

        private void LoadVendorsData(DataGridView dgv)
        {
            dgv.Rows.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // [แก้] เพิ่ม WHERE is_deleted=0
                    using (var cmd = new MySqlCommand(
                        "SELECT vendor_id, vendor_name, service_type, contact_info " +
                        "FROM vendors WHERE is_deleted = 0", conn))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            dgv.Rows.Add(reader["vendor_id"], reader["vendor_name"],
                                         reader["service_type"], reader["contact_info"]);
                }
                catch { }
            }
        }

        private void LoadAdminsData()
        {
            if (pnlAdminSettings.Controls.Find("dgvAdmins", false).Length == 0) return;
            var dgv = (DataGridView)pnlAdminSettings.Controls.Find("dgvAdmins", false)[0];
            dgv.Rows.Clear();
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT admin_id, username, admin_name FROM admins", conn))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            dgv.Rows.Add(reader["admin_id"], reader["username"], reader["admin_name"]);
                }
                catch { }
            }
        }

        // ==========================================
        // Setup Screen: Welcome
        // ==========================================
        private void SetupWelcomeScreen(Action<Panel> showScreen)
        {
            Label lblTitle = new Label()
            {
                Text = "Event Management",
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = textWhite,
                AutoSize = true
            };
            Button btnStart = new Button()
            {
                Text = "เริ่มใช้งานระบบ",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = accentBlue,
                ForeColor = textWhite,
                Size = new Size(250, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += (s, e) => showScreen(pnlRole);
            pnlWelcome.Controls.Add(lblTitle);
            pnlWelcome.Controls.Add(btnStart);
            pnlWelcome.Resize += (s, e) => {
                lblTitle.Location = new Point((pnlWelcome.Width - lblTitle.Width) / 2, pnlWelcome.Height / 3 - 50);
                btnStart.Location = new Point((pnlWelcome.Width - btnStart.Width) / 2, lblTitle.Bottom + 60);
            };
        }

        // ==========================================
        // Setup Screen: Role
        // ==========================================
        private void SetupRoleScreen(Action<Panel> showScreen)
        {
            Label lblRoleTitle = new Label()
            {
                Text = "Select Your Role",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                AutoSize = true
            };
            Panel roleContainer = new Panel() { Size = new Size(550, 200) };
            Button btnGoClient = new Button()
            {
                Text = "👤\nClient Area",
                Font = new Font("Segoe UI", 14),
                BackColor = bgCard,
                ForeColor = textWhite,
                Size = new Size(250, 150),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            Button btnGoAdmin = new Button()
            {
                Text = "⚙️\nDashboard",
                Font = new Font("Segoe UI", 14),
                BackColor = bgCard,
                ForeColor = textWhite,
                Size = new Size(250, 150),
                Location = new Point(300, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGoClient.FlatAppearance.BorderSize = 0;
            btnGoAdmin.FlatAppearance.BorderSize = 0;
            btnGoClient.Click += (s, e) => showScreen(pnlClient);
            btnGoAdmin.Click += (s, e) => showScreen(pnlLogin);
            roleContainer.Controls.Add(btnGoClient);
            roleContainer.Controls.Add(btnGoAdmin);
            pnlRole.Controls.Add(lblRoleTitle);
            pnlRole.Controls.Add(roleContainer);
            pnlRole.Resize += (s, e) => {
                lblRoleTitle.Location = new Point((pnlRole.Width - lblRoleTitle.Width) / 2, 100);
                roleContainer.Location = new Point((pnlRole.Width - roleContainer.Width) / 2, lblRoleTitle.Bottom + 50);
            };
        }

        // ==========================================
        // Setup Screen: Client (Submit Event)
        // ==========================================
        private void SetupClientScreen(Action<Panel> showScreen)
        {
            Button btnBack = new Button()
            {
                Text = "← Back",
                ForeColor = textWhite,
                BackColor = bgCard,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(30, 30),
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => showScreen(pnlRole);
            pnlClient.Controls.Add(btnBack);

            Label lblTitle = new Label()
            {
                Text = "Submit New Event Request",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = textWhite,
                AutoSize = true
            };
            pnlClient.Controls.Add(lblTitle);

            Panel formContainer = new Panel() { Width = 850, Height = 620, BackColor = bgCard };
            pnlClient.Controls.Add(formContainer);

            Label lblClientInfo = new Label()
            {
                Text = "1. Client Information",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentBlue,
                Location = new Point(20, 20),
                AutoSize = true
            };
            formContainer.Controls.Add(lblClientInfo);

            TableLayoutPanel tlpClient = new TableLayoutPanel()
            {
                Location = new Point(10, 50),
                Width = 830,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2
            };
            tlpClient.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpClient.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            TextBox txtCompany = new TextBox(), txtContact = new TextBox(),
                    txtEmail = new TextBox(), txtPhone = new TextBox();
            AddCustomInput(tlpClient, "Company / Client Name", txtCompany, 0, 0);
            AddCustomInput(tlpClient, "Contact Person", txtContact, 1, 0);
            AddCustomInput(tlpClient, "Email", txtEmail, 0, 1);
            AddCustomInput(tlpClient, "Phone Number", txtPhone, 1, 1);
            formContainer.Controls.Add(tlpClient);

            Label lblEventInfo = new Label()
            {
                Text = "2. Event Details",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentBlue,
                AutoSize = true
            };
            formContainer.Controls.Add(lblEventInfo);

            TableLayoutPanel tlpEvent = new TableLayoutPanel()
            {
                Width = 830,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 3
            };
            tlpEvent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpEvent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            TextBox txtEventName = new TextBox();
            ComboBox cmbEventType = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "Seminar", "Wedding", "Concert", "Exhibition", "Party", "Other" }
            };
            DateTimePicker dtpStart = new DateTimePicker() { Format = DateTimePickerFormat.Short };
            DateTimePicker dtpEnd = new DateTimePicker() { Format = DateTimePickerFormat.Short };
            NumericUpDown numBudget = new NumericUpDown()
            {
                Maximum = 100000000,
                DecimalPlaces = 2,
                ThousandsSeparator = true
            };
            AddCustomInput(tlpEvent, "Event Name", txtEventName, 0, 0);
            AddCustomInput(tlpEvent, "Event Type", cmbEventType, 1, 0);
            AddCustomInput(tlpEvent, "Start Date", dtpStart, 0, 1);
            AddCustomInput(tlpEvent, "End Date", dtpEnd, 1, 1);
            AddCustomInput(tlpEvent, "Budget (THB)", numBudget, 0, 2);
            formContainer.Controls.Add(tlpEvent);

            Button btnSubmit = new Button()
            {
                Text = "Submit Request",
                Size = new Size(300, 50),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            formContainer.Controls.Add(btnSubmit);

            pnlClient.Resize += (s, e) => {
                lblTitle.Location = new Point((pnlClient.Width - lblTitle.Width) / 2, 40);
                formContainer.Location = new Point((pnlClient.Width - formContainer.Width) / 2, lblTitle.Bottom + 30);
                lblEventInfo.Location = new Point(20, tlpClient.Bottom + 20);
                tlpEvent.Location = new Point(10, lblEventInfo.Bottom + 10);
                btnSubmit.Location = new Point((formContainer.Width - btnSubmit.Width) / 2, formContainer.Height - btnSubmit.Height - 30);
            };

            btnSubmit.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtCompany.Text) ||
                    string.IsNullOrWhiteSpace(txtEventName.Text) ||
                    cmbEventType.SelectedItem == null)
                {
                    MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        // ใช้ Transaction เหมือนเดิม (client + event ต้องสำเร็จพร้อมกัน)
                        using (var tr = conn.BeginTransaction())
                        {
                            string sqlClient = "INSERT INTO clients (company_name, contact_person, email, phone) " +
                                               "VALUES (@company, @contact, @email, @phone)";
                            using (var cmd = new MySqlCommand(sqlClient, conn, tr))
                            {
                                cmd.Parameters.AddWithValue("@company", txtCompany.Text);
                                cmd.Parameters.AddWithValue("@contact", txtContact.Text);
                                cmd.Parameters.AddWithValue("@email", txtEmail.Text);
                                cmd.Parameters.AddWithValue("@phone", txtPhone.Text);
                                cmd.ExecuteNonQuery();
                            }
                            long newClientId;
                            using (var cmdId = new MySqlCommand("SELECT LAST_INSERT_ID()", conn, tr))
                                newClientId = Convert.ToInt64(cmdId.ExecuteScalar());

                            // [เพิ่ม] เรียก SP sp_create_event แทน INSERT ตรงๆ
                            using (var cmdSP = new MySqlCommand("sp_create_event", conn, tr))
                            {
                                cmdSP.CommandType = System.Data.CommandType.StoredProcedure;
                                cmdSP.Parameters.AddWithValue("p_client_id", newClientId);
                                cmdSP.Parameters.AddWithValue("p_event_name", txtEventName.Text);
                                cmdSP.Parameters.AddWithValue("p_event_type", cmbEventType.SelectedItem.ToString());
                                cmdSP.Parameters.AddWithValue("p_start_date", dtpStart.Value.ToString("yyyy-MM-dd"));
                                cmdSP.Parameters.AddWithValue("p_end_date", dtpEnd.Value.ToString("yyyy-MM-dd"));
                                cmdSP.Parameters.AddWithValue("p_budget", numBudget.Value);
                                var pId = cmdSP.Parameters.Add("p_new_event_id", MySqlDbType.Int32);
                                var pMsg = cmdSP.Parameters.Add("p_message", MySqlDbType.VarChar, 255);
                                pId.Direction = System.Data.ParameterDirection.Output;
                                pMsg.Direction = System.Data.ParameterDirection.Output;
                                cmdSP.ExecuteNonQuery();

                                string spMsg = pMsg.Value?.ToString() ?? "";
                                if (spMsg.StartsWith("ERROR"))
                                {
                                    tr.Rollback();
                                    MessageBox.Show(spMsg, "ไม่สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                            tr.Commit();
                            MessageBox.Show($"ส่งคำขอเรียบร้อย! รหัสลูกค้า: {newClientId}",
                                "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtCompany.Clear(); txtContact.Clear(); txtEmail.Clear(); txtPhone.Clear();
                            txtEventName.Clear(); numBudget.Value = 0; cmbEventType.SelectedIndex = -1;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
        }

        // ==========================================
        // Setup Screen: Login
        // [แก้] เปลี่ยนจาก plain text เป็น BCrypt.Verify
        // ==========================================
        private void SetupLoginScreen(Action<Panel> showScreen)
        {
            Button btnBack = new Button()
            {
                Text = "← Back",
                ForeColor = textWhite,
                BackColor = bgCard,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(30, 30),
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => showScreen(pnlRole);
            pnlLogin.Controls.Add(btnBack);

            Panel loginContainer = new Panel() { Width = 400, Height = 450 };
            pnlLogin.Controls.Add(loginContainer);

            loginContainer.Controls.Add(new Label()
            {
                Text = "System Login",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                AutoSize = true,
                Location = new Point(70, 20)
            });
            loginContainer.Controls.Add(new Label()
            {
                Text = "Username",
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 12),
                Location = new Point(40, 100),
                AutoSize = true
            });
            TextBox txtUser = new TextBox()
            {
                Location = new Point(40, 130),
                Size = new Size(320, 35),
                Font = new Font("Segoe UI", 14),
                BackColor = inputBg,
                ForeColor = textWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
            loginContainer.Controls.Add(txtUser);

            loginContainer.Controls.Add(new Label()
            {
                Text = "Password",
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 12),
                Location = new Point(40, 200),
                AutoSize = true
            });
            TextBox txtPass = new TextBox()
            {
                Location = new Point(40, 230),
                Size = new Size(320, 35),
                Font = new Font("Segoe UI", 14),
                BackColor = inputBg,
                ForeColor = textWhite,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };
            loginContainer.Controls.Add(txtPass);

            Button btnLogin = new Button()
            {
                Text = "Log In",
                Location = new Point(40, 320),
                Size = new Size(320, 50),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;

            btnLogin.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text)) return;

                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        // [แก้] ดึง password hash และ admin_name แยกกัน
                        // ไม่เปรียบเทียบ password ใน SQL อีกต่อไป
                        string sql = "SELECT admin_name, password FROM admins WHERE username = @u";
                        using (var cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@u", txtUser.Text);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string adminName = reader["admin_name"].ToString();
                                    string storedHash = reader["password"].ToString();

                                    // [เพิ่ม] ตรวจสอบด้วย BCrypt.Verify
                                    bool passwordOk = BCrypt.Net.BCrypt.Verify(txtPass.Text, storedHash);

                                    if (passwordOk)
                                    {
                                        lblAdminHeaderTitle.Text = $"Admin Workspace | Welcome, {adminName}";
                                        LoadEventsData();
                                        LoadAdminsData();
                                        pnlAdminStaff.Visible = false;
                                        pnlAdminVendors.Visible = false;
                                        pnlAdminSettings.Visible = false;
                                        pnlAdminBudget.Visible = false;
                                        pnlAdminAssignments.Visible = false;
                                        pnlAdminOverview.Visible = true;
                                        pnlAdminOverview.BringToFront();
                                        txtUser.Clear(); txtPass.Clear();
                                        showScreen(pnlAdmin);
                                    }
                                    else
                                    {
                                        MessageBox.Show("Username หรือ Password ไม่ถูกต้อง!",
                                            "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Username หรือ Password ไม่ถูกต้อง!",
                                        "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database Error: " + ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            loginContainer.Controls.Add(btnLogin);
            pnlLogin.Resize += (s, e) =>
                loginContainer.Location = new Point(
                    (pnlLogin.Width - loginContainer.Width) / 2,
                    (pnlLogin.Height - loginContainer.Height) / 2);
        }

        // ==========================================
        // Setup Screen: Staff & Vendors
        // ==========================================
        private void SetupAdminStaffAndVendors()
        {
            // --- Staff ---
            pnlAdminStaff.Controls.Clear();
            pnlAdminStaff.AutoScroll = true;

            pnlAdminStaff.Controls.Add(new Label()
            {
                Text = "Staff Management",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(40, 30),
                AutoSize = true
            });

            DataGridView dgvStaff = CreateStyledDataGrid();
            dgvStaff.Location = new Point(40, 90); dgvStaff.Height = 250;
            dgvStaff.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "ID", FillWeight = 10 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "First Name", FillWeight = 25 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Name", FillWeight = 25 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Department", FillWeight = 20 });
            dgvStaff.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Role", FillWeight = 20 });
            pnlAdminStaff.Controls.Add(dgvStaff);

            Panel addStaffPanel = new Panel()
            {
                Location = new Point(40, 360),
                Height = 260,
                BackColor = bgCard,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlAdminStaff.Controls.Add(addStaffPanel);

            TableLayoutPanel tlpStaff = new TableLayoutPanel()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2
            };
            tlpStaff.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpStaff.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            addStaffPanel.Controls.Add(tlpStaff);

            TextBox txtFName = new TextBox(), txtLName = new TextBox(), txtRole = new TextBox();
            ComboBox cmbDept = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "Operation", "Technical", "Sales", "Management" }
            };
            AddCustomInput(tlpStaff, "First Name", txtFName, 0, 0);
            AddCustomInput(tlpStaff, "Last Name", txtLName, 1, 0);
            AddCustomInput(tlpStaff, "Department", cmbDept, 0, 1);
            AddCustomInput(tlpStaff, "Role", txtRole, 1, 1);

            Button btnAddStaff = new Button()
            {
                Text = "Save Staff",
                Size = new Size(150, 45),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddStaff.FlatAppearance.BorderSize = 0;
            addStaffPanel.Controls.Add(btnAddStaff);

            pnlAdminStaff.Resize += (s, e) => {
                dgvStaff.Width = pnlAdminStaff.Width - 80;
                addStaffPanel.Width = pnlAdminStaff.Width - 80;
                btnAddStaff.Location = new Point(addStaffPanel.Width - btnAddStaff.Width - 20,
                                                 addStaffPanel.Height - btnAddStaff.Height - 15);
            };

            btnAddStaff.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtFName.Text) || string.IsNullOrWhiteSpace(txtLName.Text))
                {
                    MessageBox.Show("กรุณากรอกชื่อ-นามสกุล", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (var cmd = new MySqlCommand(
                            "INSERT INTO employees (first_name, last_name, department, role) " +
                            "VALUES (@f, @l, @d, @r)", conn))
                        {
                            cmd.Parameters.AddWithValue("@f", txtFName.Text);
                            cmd.Parameters.AddWithValue("@l", txtLName.Text);
                            cmd.Parameters.AddWithValue("@d", cmbDept.SelectedItem?.ToString());
                            cmd.Parameters.AddWithValue("@r", txtRole.Text);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("เพิ่มพนักงานสำเร็จ!", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtFName.Clear(); txtLName.Clear(); cmbDept.SelectedIndex = -1; txtRole.Clear();
                        LoadStaffData(dgvStaff);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            // --- Vendors ---
            pnlAdminVendors.Controls.Clear();
            pnlAdminVendors.AutoScroll = true;

            pnlAdminVendors.Controls.Add(new Label()
            {
                Text = "Vendors Management",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(40, 30),
                AutoSize = true
            });

            DataGridView dgvVendors = CreateStyledDataGrid();
            dgvVendors.Location = new Point(40, 90); dgvVendors.Height = 250;
            dgvVendors.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvVendors.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "ID", FillWeight = 10 });
            dgvVendors.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Vendor Name", FillWeight = 30 });
            dgvVendors.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Service Type", FillWeight = 30 });
            dgvVendors.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Contact Info", FillWeight = 30 });
            pnlAdminVendors.Controls.Add(dgvVendors);

            Panel addVendorPanel = new Panel()
            {
                Location = new Point(40, 360),
                Height = 200,
                BackColor = bgCard,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlAdminVendors.Controls.Add(addVendorPanel);

            TableLayoutPanel tlpVendor = new TableLayoutPanel()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1
            };
            tlpVendor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpVendor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpVendor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            addVendorPanel.Controls.Add(tlpVendor);

            TextBox txtVName = new TextBox(), txtVContact = new TextBox();
            ComboBox cmbVType = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "Light & Sound", "Decoration", "Catering", "Location", "Other" }
            };
            AddCustomInput(tlpVendor, "Vendor Name", txtVName, 0, 0);
            AddCustomInput(tlpVendor, "Service Type", cmbVType, 1, 0);
            AddCustomInput(tlpVendor, "Contact Info", txtVContact, 2, 0);

            Button btnAddVendor = new Button()
            {
                Text = "Save Vendor",
                Size = new Size(150, 45),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddVendor.FlatAppearance.BorderSize = 0;
            addVendorPanel.Controls.Add(btnAddVendor);

            pnlAdminVendors.Resize += (s, e) => {
                dgvVendors.Width = pnlAdminVendors.Width - 80;
                addVendorPanel.Width = pnlAdminVendors.Width - 80;
                btnAddVendor.Location = new Point(addVendorPanel.Width - btnAddVendor.Width - 20,
                                                  addVendorPanel.Height - btnAddVendor.Height - 15);
            };

            btnAddVendor.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtVName.Text))
                {
                    MessageBox.Show("กรุณากรอกชื่อ Vendor", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (var cmd = new MySqlCommand(
                            "INSERT INTO vendors (vendor_name, service_type, contact_info) VALUES (@n, @s, @c)", conn))
                        {
                            cmd.Parameters.AddWithValue("@n", txtVName.Text);
                            cmd.Parameters.AddWithValue("@s", cmbVType.SelectedItem?.ToString());
                            cmd.Parameters.AddWithValue("@c", txtVContact.Text);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("เพิ่ม Vendor สำเร็จ!", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtVName.Clear(); cmbVType.SelectedIndex = -1; txtVContact.Clear();
                        LoadVendorsData(dgvVendors);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            LoadStaffData(dgvStaff);
            LoadVendorsData(dgvVendors);
        }

        // ==========================================
        // Setup Screen: System Settings
        // - เพิ่ม Admin พร้อม BCrypt hash
        // - ลบ Admin พร้อมป้องกันลบคนสุดท้าย
        // ==========================================
        private void SetupSystemSettings(Action<Panel> showScreen)
        {
            pnlAdminSettings.Controls.Clear();
            pnlAdminSettings.AutoScroll = true;

            pnlAdminSettings.Controls.Add(new Label()
            {
                Text = "System Settings (Admin Management)",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(40, 30),
                AutoSize = true
            });

            // หัวข้อ + ปุ่มลบอยู่แถวเดียวกัน
            pnlAdminSettings.Controls.Add(new Label()
            {
                Text = "Admin Users:",
                Font = new Font("Segoe UI", 14),
                ForeColor = textMuted,
                Location = new Point(40, 90),
                AutoSize = true
            });

            // ปุ่มลบ Admin — วางขวามือ จัดตำแหน่งตอน Resize
            Button btnDeleteAdmin = new Button()
            {
                Text = "🗑  Delete Selected Admin",
                Size = new Size(220, 40),
                BackColor = dangerRed,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnDeleteAdmin.FlatAppearance.BorderSize = 0;
            pnlAdminSettings.Controls.Add(btnDeleteAdmin);

            DataGridView dgvAdmins = CreateStyledDataGrid();
            dgvAdmins.Name = "dgvAdmins";
            dgvAdmins.Location = new Point(40, 130);
            dgvAdmins.Height = 200;
            dgvAdmins.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvAdmins.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Admin ID", FillWeight = 20 });
            dgvAdmins.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Username", FillWeight = 30 });
            dgvAdmins.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Display Name", FillWeight = 50 });
            pnlAdminSettings.Controls.Add(dgvAdmins);

            // ——— ปุ่มลบ: logic ——————————————————————————————
            btnDeleteAdmin.Click += (s, e) =>
            {
                if (dgvAdmins.SelectedRows.Count == 0)
                {
                    MessageBox.Show("กรุณาคลิกเลือก Admin ที่ต้องการลบก่อน", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string targetId = dgvAdmins.SelectedRows[0].Cells[0].Value.ToString();
                string targetName = dgvAdmins.SelectedRows[0].Cells[2].Value.ToString();

                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();

                        // ป้องกันลบ admin คนสุดท้าย — ต้องมีอย่างน้อย 1 คนเสมอ
                        int totalAdmins = 0;
                        using (var cmdCount = new MySqlCommand("SELECT COUNT(*) FROM admins", conn))
                            totalAdmins = Convert.ToInt32(cmdCount.ExecuteScalar());

                        if (totalAdmins <= 1)
                        {
                            MessageBox.Show("ไม่สามารถลบได้ — ต้องมี Admin อย่างน้อย 1 คนในระบบ",
                                "ไม่อนุญาต", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // ยืนยันก่อนลบจริง
                        var confirm = MessageBox.Show(
                            $"ยืนยันการลบ Admin:\n\nID: {targetId}\nชื่อ: {targetName}\n\nการลบนี้ไม่สามารถเรียกคืนได้",
                            "ยืนยันการลบ",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (confirm != DialogResult.Yes) return;

                        using (var cmd = new MySqlCommand(
                            "DELETE FROM admins WHERE admin_id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", targetId);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"ลบ Admin \"{targetName}\" เรียบร้อยแล้ว",
                            "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadAdminsData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            // ——— ส่วนเพิ่ม Admin ——————————————————————————————
            pnlAdminSettings.Controls.Add(new Label()
            {
                Text = "➕ Add New Admin",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = accentBlue,
                Location = new Point(40, 360),
                AutoSize = true
            });

            Panel addAdminPanel = new Panel()
            {
                Location = new Point(40, 400),
                Height = 160,
                BackColor = bgCard,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlAdminSettings.Controls.Add(addAdminPanel);

            TableLayoutPanel tlpAdmin = new TableLayoutPanel()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 10, 10, 0)
            };
            tlpAdmin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpAdmin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpAdmin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            addAdminPanel.Controls.Add(tlpAdmin);

            TextBox txtNewUser = new TextBox(),
                    txtNewPass = new TextBox() { UseSystemPasswordChar = true },
                    txtNewName = new TextBox();
            AddCustomInput(tlpAdmin, "Username", txtNewUser, 0, 0);
            AddCustomInput(tlpAdmin, "Password", txtNewPass, 1, 0);
            AddCustomInput(tlpAdmin, "Admin Name", txtNewName, 2, 0);

            Button btnAddAdmin = new Button()
            {
                Text = "Save Admin",
                Size = new Size(150, 45),
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddAdmin.FlatAppearance.BorderSize = 0;
            addAdminPanel.Controls.Add(btnAddAdmin);

            // Resize: จัดตำแหน่งปุ่มลบขวาบน + ปุ่มบันทึกขวาล่าง
            pnlAdminSettings.Resize += (s, e) =>
            {
                btnDeleteAdmin.Location = new Point(pnlAdminSettings.Width - btnDeleteAdmin.Width - 40, 85);
                dgvAdmins.Width = pnlAdminSettings.Width - 80;
                addAdminPanel.Width = pnlAdminSettings.Width - 80;
                btnAddAdmin.Location = new Point(addAdminPanel.Width - btnAddAdmin.Width - 20,
                                                     addAdminPanel.Height - btnAddAdmin.Height - 15);
            };

            // ——— ปุ่มบันทึก Admin ใหม่ ————————————————————————
            btnAddAdmin.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNewUser.Text) ||
                    string.IsNullOrWhiteSpace(txtNewPass.Text) ||
                    string.IsNullOrWhiteSpace(txtNewName.Text))
                {
                    MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน!", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ตรวจความยาว password ขั้นต่ำ 6 ตัว
                if (txtNewPass.Text.Length < 6)
                {
                    MessageBox.Show("Password ต้องมีอย่างน้อย 6 ตัวอักษร", "แจ้งเตือน",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();

                        // Auto-generate running ID: AD001, AD002, ...
                        string newAdminId = "AD001";
                        using (var cmdId = new MySqlCommand(
                            "SELECT admin_id FROM admins ORDER BY admin_id DESC LIMIT 1", conn))
                        {
                            object result = cmdId.ExecuteScalar();
                            if (result != null && result.ToString().StartsWith("AD"))
                            {
                                if (int.TryParse(result.ToString().Substring(2), out int lastNum))
                                    newAdminId = "AD" + (lastNum + 1).ToString("D3");
                            }
                        }

                        // Hash password ด้วย BCrypt workFactor=12 ก่อน INSERT
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(txtNewPass.Text, workFactor: 12);

                        using (var cmd = new MySqlCommand(
                            "INSERT INTO admins (admin_id, username, password, admin_name) " +
                            "VALUES (@id, @u, @p, @name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", newAdminId);
                            cmd.Parameters.AddWithValue("@u", txtNewUser.Text);
                            cmd.Parameters.AddWithValue("@p", hashedPassword);
                            cmd.Parameters.AddWithValue("@name", txtNewName.Text);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show(
                            $"เพิ่ม Admin สำเร็จ!\n\nรหัส: {newAdminId}\nUsername: {txtNewUser.Text}\n" +
                            $"ชื่อ: {txtNewName.Text}\n\nสามารถ Login เข้าระบบได้ทันที",
                            "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        txtNewUser.Clear(); txtNewPass.Clear(); txtNewName.Clear();
                        LoadAdminsData();
                    }
                    catch (MySqlException ex) when (ex.Number == 1062)
                    {
                        MessageBox.Show("Username นี้มีคนใช้ไปแล้ว กรุณาตั้งชื่ออื่น", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("เกิดข้อผิดพลาด: " + ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            LoadAdminsData();
        }

        // ==========================================
        // [แก้] UpdateEventStatus → เรียก SP แทน UPDATE ตรงๆ
        // sp_update_event_status ตรวจลำดับ status ให้อัตโนมัติ
        // ==========================================
        private void UpdateEventStatusViaSP(string newStatus, string currentFilter = "All")
        {
            if (dgvPending.SelectedRows.Count == 0)
            {
                MessageBox.Show("กรุณาคลิกเลือกรายการก่อน", "แจ้งเตือน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string eventId = dgvPending.SelectedRows[0].Cells[0].Value.ToString();

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_update_event_status", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_event_id", int.Parse(eventId));
                        cmd.Parameters.AddWithValue("p_new_status", newStatus);
                        var outMsg = cmd.Parameters.Add("p_message", MySqlDbType.VarChar, 255);
                        outMsg.Direction = System.Data.ParameterDirection.Output;
                        cmd.ExecuteNonQuery();

                        string msg = outMsg.Value?.ToString() ?? "";
                        if (msg.StartsWith("SUCCESS"))
                        {
                            MessageBox.Show(msg, "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadEventsData(currentFilter);
                        }
                        else
                            MessageBox.Show(msg, "ไม่สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        // ==========================================
        // UI Helpers
        // ==========================================
        private Button CreateAdminMenuButton(string text, int yPos)
        {
            Button btn = new Button()
            {
                Text = text,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = bgSidebar,
                Size = new Size(280, 60),
                Location = new Point(0, yPos),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private DataGridView CreateStyledDataGrid()
        {
            DataGridView dgv = new DataGridView();
            dgv.BackgroundColor = bgBase; dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = bgCard;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = textWhite;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 50;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.GridColor = gridLineColor;
            dgv.DefaultCellStyle.BackColor = bgBase;
            dgv.DefaultCellStyle.ForeColor = textWhite;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            dgv.DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowHeadersVisible = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AllowUserToAddRows = false; dgv.AllowUserToResizeRows = false;
            dgv.RowTemplate.Height = 45;
            return dgv;
        }

        private void AddCustomInput(TableLayoutPanel tlp, string labelText, Control inputCtrl, int col, int row)
        {
            Panel pnl = new Panel() { Dock = DockStyle.Fill, Height = 65, Margin = new Padding(10) };
            Label lbl = new Label()
            {
                Text = labelText,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 10),
                Location = new Point(0, 0),
                AutoSize = true
            };
            inputCtrl.Location = new Point(0, 25);
            inputCtrl.Font = new Font("Segoe UI", 12);
            inputCtrl.BackColor = inputBg;
            inputCtrl.ForeColor = textWhite;
            pnl.Resize += (s, e) => inputCtrl.Width = pnl.Width - 10;
            if (inputCtrl is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
            else if (inputCtrl is NumericUpDown nud) nud.BorderStyle = BorderStyle.FixedSingle;
            else if (inputCtrl is ComboBox cb) cb.FlatStyle = FlatStyle.Flat;
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(inputCtrl);
            tlp.Controls.Add(pnl, col, row);
        }
    }
}
