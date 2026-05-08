# Database_11_project
1 .txtอัพใส่SQL สิ่งที่เพิ่ม(
  1.เพิ่มcreated_at และ updated_at ทุกตาราง(เวลาสร้างหรือเพิ่มข้อมูลครั้งแรกและการแก้ไขล่าสุด) 
  2.events → clients ใช้ RESTRICT แทน CASCADEถ้าใช้ CASCADE แล้วลบ clientทางeventจะหายหมด 
  3.event_expenses → vendors ใช้ SET NULL เพราะ vendor ถูกลบได้ แต่ประวัติค่าใช้จ่ายต้องยังอยู่เพื่อ audit งบ
2.เอาไฟล์.csไปแทนอีกไฟล์
3.เอาไปแทนที่ public Form1() ก่อนเพื่อสร้างadmin01 รหัส 1234 แล้วค่อยลบออกเอาอันก่อนหน้ากลับมาทำครั้งเดียว(ทำพื่อให้hashข้อมูลรหัสไม่เก็บลงdatabaseกันเหนียวหน่อย)
4.ยังไม่ได้เพิ่มอะไรจะเพิ่มหรือไม่แล้วแต่เลยและพวกฟังก์ชันด้านล่างที่เพิ่มมาก็ไม่ได้โชว์ให้เห็นที่Frontend
===start===
public Form1()
{
    InitializeComponent();
    this.Text = "Event Management System";
    this.WindowState = FormWindowState.Maximized;
    
    // ====== โค้ดชั่วคราว — ลบออกหลังใช้งานแล้ว ======
    string hash = BCrypt.Net.BCrypt.HashPassword("1234", workFactor: 12);
    using (var conn = new MySqlConnection(connectionString))
    {
        conn.Open();
        // ลบ admin เก่าทั้งหมด
        new MySqlCommand("DELETE FROM admins", conn).ExecuteNonQuery();
        // สร้างใหม่
        var cmd = new MySqlCommand(
            "INSERT INTO admins (admin_id, username, password, admin_name) VALUES ('AD001','admin01',@p,'ผู้ดูแล')", conn);
        cmd.Parameters.AddWithValue("@p", hash);
        cmd.ExecuteNonQuery();
    }
    MessageBox.Show("Reset สำเร็จ! Username: admin01 | Password: 1234");
    // ====== จบโค้ดชั่วคราว ======
    
    InitializeCustomUI();
}
===end===
  == view function transaction ==
================================================================
ส่วนที่ 4 : Views
================================================================
 
────────────────────────────────────────────────────────────────
4.1  v_active_events
────────────────────────────────────────────────────────────────
 
  ดึง: งานทั้งหมดที่ is_deleted=0 JOIN ชื่อลูกค้า
 
  ทำไมต้องมี:
    ทุก query ที่ดึงงานต้องมี WHERE is_deleted=0 เสมอ
    ถ้าลืมครั้งเดียว งานที่ลบแล้วจะโชว์ขึ้นมาหน้าบ้าน
    ใส่ไว้ใน View แค่ครั้งเดียว C# SELECT * FROM v_active_events ได้เลย
 
  C# ใช้งาน:
    SELECT * FROM v_active_events
    SELECT * FROM v_active_events WHERE status = 'Ongoing'
 
────────────────────────────────────────────────────────────────
4.2  v_event_budget_summary
────────────────────────────────────────────────────────────────
 
  ดึง: budget / total_spent / budget_remaining ของทุกงาน
  คำนวณ: budget_remaining = budget - SUM(expenses)
 
  ทำไมต้องมี:
    ต้อง JOIN 2 ตาราง + GROUP BY + SUM + COALESCE ทุกครั้ง
    โอกาสผิดพลาดสูง เช่น ลืม COALESCE ทำให้ได้ NULL แทน 0
 
  C# ใช้งาน:
    SELECT * FROM v_event_budget_summary ORDER BY event_id DESC
 
────────────────────────────────────────────────────────────────
4.3  v_employee_workload
────────────────────────────────────────────────────────────────
 
  ดึง: พนักงานแต่ละคน + จำนวนงาน active ที่รับอยู่
 
  ทำไมต้องมี:
    ก่อน assign พนักงาน ดูได้ทันทีว่าใครยังว่าง
    ใช้ GROUP_CONCAT แสดงรายชื่องานในช่องเดียว
 
  C# ใช้งาน:
    SELECT * FROM v_employee_workload WHERE active_event_count = 0
 
 
================================================================
ส่วนที่ 5 : Stored Functions
================================================================
 
  ทำไมใช้ Function ไม่ใช้ Procedure:
    Function คืนค่าได้ใน SELECT โดยตรง
    เช่น SELECT fn_get_budget_remaining(2) → ได้ 45000.00 ทันที
    Procedure ต้องใช้ OUT parameter แยก เรียกใช้ยากกว่า
    นอกจากนี้ Function ยังเรียกได้จากใน Trigger และ SP อื่นได้ด้วย
 
────────────────────────────────────────────────────────────────
5.1  fn_get_budget_remaining(event_id)
────────────────────────────────────────────────────────────────
 
  คืนค่า: DECIMAL — งบคงเหลือ = budget - SUM(expenses)
           NULL ถ้า event ไม่มีอยู่หรือถูกลบ
 
  SP sp_add_expense เรียกใช้ Function นี้ตรวจงบก่อน INSERT
  Trigger trg_before_expense_insert เรียกใช้ Function นี้เช่นกัน
  ไม่ต้องเขียน logic คำนวณงบซ้ำในหลายที่
 
────────────────────────────────────────────────────────────────
5.2  fn_count_active_employees_in_event(event_id)
────────────────────────────────────────────────────────────────
 
  คืนค่า: INT — จำนวนพนักงาน active ในงานนั้น
 
  C# ใช้เช็คก่อน assign ว่างานนี้มีคนพอแล้วหรือยัง
  SELECT fn_count_active_employees_in_event(1)
 
 
================================================================
ส่วนที่ 6 : Stored Procedures + Transactions
================================================================
 
────────────────────────────────────────────────────────────────
6.1  sp_create_event  (มี Transaction)
────────────────────────────────────────────────────────────────
 
  รับ: client_id, event_name, event_type, start_date, end_date, budget
  คืน: p_new_event_id (OUT), p_message (OUT)
 
  Validate ก่อน:
    ชื่องานห้ามว่าง
    end_date ต้องไม่ก่อน start_date
 
  Transaction ใช้เพราะ:
    ถ้า INSERT สำเร็จแต่มีอะไร fail ถัดไป
    ต้อง ROLLBACK ทั้งหมด ไม่ให้มีข้อมูลกลางคัน
 
  C# เรียก: CALL sp_create_event(..., @id, @msg)
 
────────────────────────────────────────────────────────────────
6.2  sp_soft_delete_event  (มี Transaction)
────────────────────────────────────────────────────────────────
 
  รับ: event_id, admin_id
  คืน: p_message (OUT)
 
  ป้องกัน: ลบงานที่ status = Ongoing ไม่ได้
 
  Transaction ใช้เพราะ:
    ต้อง UPDATE events + UPDATE ข้อมูลที่เกี่ยวข้องพร้อมกัน
    ถ้า UPDATE แรกสำเร็จแต่ที่สอง fail
    จะเกิดสถานะ event ถูกลบแต่ assignment ยังค้างอยู่ ข้อมูลพัง
 
────────────────────────────────────────────────────────────────
6.3  sp_update_event_status
────────────────────────────────────────────────────────────────
 
  รับ: event_id, new_status
  คืน: p_message (OUT)
 
  บังคับลำดับ:
    Pitching → Preparing → Ongoing → Completed
    ทุก status → Cancelled ได้ (ยกเว้น Completed)
 
  ทำไมต้องมี SP ไม่ใช้ UPDATE ตรงๆ:
    UPDATE events SET status='Completed' WHERE event_id=1
    สามารถข้ามจาก Pitching ไป Completed ได้เลย ไม่สะท้อนความจริง
    SP บังคับให้ทำตามลำดับขั้นตอนงานจริง
 
  C# เรียก: CALL sp_update_event_status(1, 'Preparing', @msg)
 
────────────────────────────────────────────────────────────────
6.4  sp_assign_employee
────────────────────────────────────────────────────────────────
 
  รับ: event_id, employee_id, task_role
  คืน: p_message (OUT)
 
  เช็ค 3 อย่างก่อน INSERT:
    1. event มีอยู่จริงและไม่ถูกลบ
    2. employee มีอยู่จริงและไม่ถูกลบ
    3. ยังไม่ได้ assign คู่นี้ไปแล้ว (กัน duplicate)
 
────────────────────────────────────────────────────────────────
6.5  sp_add_expense  (มี Transaction)
────────────────────────────────────────────────────────────────
 
  รับ: event_id, vendor_id, amount, expense_details, expense_date
  คืน: p_message (OUT)
 
  Transaction ใช้ป้องกัน Race Condition:
    ถ้าไม่มี Transaction:
      User A อ่านงบเหลือ 50,000 บาท
      User B อ่านงบเหลือ 50,000 บาท พร้อมกัน
      User A INSERT 40,000 สำเร็จ
      User B INSERT 40,000 สำเร็จ (ยังเห็นว่าพอ)
      รวมใช้ไป 80,000 แต่งบมีแค่ 50,000 งบเกินโดยไม่รู้ตัว
    ถ้ามี Transaction:
      User B ต้องรอ User A ทำเสร็จก่อน
      อ่านงบใหม่ได้ 10,000 เช็คถูกต้อง
 
 
================================================================
ส่วนที่ 7 : Triggers
================================================================
 
  ทำไม Trigger ต้องมีทั้งที่ SP เช็คอยู่แล้ว:
    SP เช็คได้เฉพาะเมื่อเรียกผ่าน CALL เท่านั้น
    ถ้า developer INSERT ตรงผ่าน MySQL Workbench
    หรือเขียน C# INSERT ไม่ผ่าน SP
    Trigger ยังทำงานอยู่ดี ไม่มีทางข้ามได้
    SP = ประตูหน้า / Trigger = รั้วรอบกันทุกทิศทาง
 
────────────────────────────────────────────────────────────────
7.1  trg_before_expense_insert
     BEFORE INSERT บน event_expenses
────────────────────────────────────────────────────────────────
 
  ทำงาน: เรียก fn_get_budget_remaining()
          ถ้าค่าใช้จ่ายใหม่ > งบคงเหลือ → SIGNAL error หยุด INSERT
 
  ถ้าไม่มี:
    INSERT ตรงเข้า event_expenses ที่ทำให้เกินงบ → สำเร็จ
    งบเกินโดยไม่มีอะไรหยุด ทั้งที่ SP ป้องกันไว้แล้ว
 
────────────────────────────────────────────────────────────────
7.2  trg_before_event_date_update
     BEFORE UPDATE บน events
────────────────────────────────────────────────────────────────
 
  ทำงาน: ถ้า end_date < start_date → SIGNAL error หยุด UPDATE
 
  ถ้าไม่มี:
    UPDATE events SET end_date='2026-04-01' WHERE event_id=1
    (start_date คือ 2026-05-02)
    สำเร็จ งานมี end_date ก่อน start_date
    ระบบคำนวณระยะเวลางานผิด รายงานผิดทั้งหมด 
  )
