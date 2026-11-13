# MAGIC ERP — MODULE 12: HUMAN RESOURCE MANAGEMENT SPECIFICATION
## Staff Directory, Attendance, Leave Management & Employee Lifecycle

**Route:** `/billing/hr`  
**API Controller:** `HrController` (`/api/v1/hr`)  
**Database Schema:** `hr`

---

## 1. Functional Scope

1. **Staff Directory & Organizational Structure:**
   - Unified employee directory with job designations, branch allocations, reporting hierarchy, and emergency contacts.
   - Bank details and PAN capture for automated payroll dispatches.
   - Self-service portal linking employee record to platform user account.
2. **Attendance Tracking:**
   - Daily punch-in and punch-out recording work duration.
   - Status classifications: `Present`, `Absent`, `HalfDay`, `OnLeave`, `Holiday`.
   - Supervisor-authorized corrections with mandatory notes and audit logs.
3. **Leave Management:**
   - Configurable leave types (Casual Leave, Sick Leave, Earned Leave).
   - Real-time leave balance tracking across fiscal years.
   - Application submission, manager review, and approval/rejection workflows.
4. **Lifecycle Events:**
   - Tracks onboarding, promotions, department transfers, and exit processing.

