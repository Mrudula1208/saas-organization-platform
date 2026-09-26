-- ==============================================================================
-- Development Seed Script for Multi-Tenant SaaS Organization Platform
-- File: Database/Seed/DevelopmentData.sql
-- 
-- Description:
-- Populates the development database with realistic test data for:
--  - 1 Tenant Organization: "TechNova Solutions" (Pro Subscription Plan)
--  - 5 Indian Users with distinct valid roles (TenantAdmin, Manager, Member)
--    All users have password: Password123!
--    BCrypt Hash: $2a$11$dDSV7jS02005iff2MU5G/O2quanliAZHqKuY81XXnMmgYpGcWgZRi
--  - 3 Realistic Software Projects (Employee Management, Online Booking, Inventory)
--  - 10 Project Member relationship mappings
--  - 12 Tasks distributed across "To Do", "In Progress", and "Completed"
--  - 2 Payment transactions (UPI & Card billing records)
--  - 4 Workspace notifications (read and unread)
--  - 2 Development report records
--  - 5 Realistic system/audit logs
--
-- Safety:
--  - Uses deterministic GUIDs for reliable relationship binding.
--  - Idempotent: checks IF NOT EXISTS before every insertion to prevent duplicate keys.
--  - Strictly honors multi-tenant isolation, EF Core foreign keys, and column types.
-- ==============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- -----------------------------------------------------------------------------
-- 0. Variable Declarations & References
-- -----------------------------------------------------------------------------
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Existing Seed Subscription Plan: Pro Plan ($45/mo)
DECLARE @ProPlanId UNIQUEIDENTIFIER = 'cccc1111-2222-3333-4444-555566667777';

-- Tenant ID
DECLARE @TenantId UNIQUEIDENTIFIER = '11111111-aaaa-bbbb-cccc-000000000001';

-- User IDs
DECLARE @User_RahulPatil UNIQUEIDENTIFIER = '22222222-aaaa-bbbb-cccc-000000000001';
DECLARE @User_SnehaMore UNIQUEIDENTIFIER = '22222222-aaaa-bbbb-cccc-000000000002';
DECLARE @User_AkashJadhav UNIQUEIDENTIFIER = '22222222-aaaa-bbbb-cccc-000000000003';
DECLARE @User_NehaKulkarni UNIQUEIDENTIFIER = '22222222-aaaa-bbbb-cccc-000000000004';
DECLARE @User_RohanShah UNIQUEIDENTIFIER = '22222222-aaaa-bbbb-cccc-000000000005';

-- Common BCrypt hash for "Password123!"
DECLARE @PasswordHash NVARCHAR(MAX) = N'$2a$11$dDSV7jS02005iff2MU5G/O2quanliAZHqKuY81XXnMmgYpGcWgZRi';

-- Project IDs
DECLARE @Proj_EmployeeMgmt UNIQUEIDENTIFIER = '33333333-aaaa-bbbb-cccc-000000000001';
DECLARE @Proj_BookingSystem UNIQUEIDENTIFIER = '33333333-aaaa-bbbb-cccc-000000000002';
DECLARE @Proj_Inventory UNIQUEIDENTIFIER = '33333333-aaaa-bbbb-cccc-000000000003';

-- -----------------------------------------------------------------------------
-- 1. Tenant Organization: TechNova Solutions
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [Tenants] WHERE [Id] = @TenantId)
BEGIN
    INSERT INTO [Tenants] (
        [Id],
        [Name],
        [Domain],
        [ContactEmail],
        [ContactPhone],
        [SubscriptionPlanId],
        [IsActive],
        [LogoImageUrl],
        [EmailNotificationsEnabled],
        [InAppNotificationsEnabled],
        [IsDeleted],
        [CreatedAt]
    )
    VALUES (
        @TenantId,
        N'TechNova Solutions',
        N'technova',
        N'admin@technova.com',
        N'+91 98201 12345',
        @ProPlanId,
        1,
        NULL,
        1,
        1,
        0,
        DATEADD(DAY, -30, @Now)
    );
    PRINT 'Inserted Tenant: TechNova Solutions';
END;

-- -----------------------------------------------------------------------------
-- 2. Users (5 Realistic Accounts with Roles: TenantAdmin, Manager, Member)
-- -----------------------------------------------------------------------------

-- 2.1 Rahul Patil - Tenant Administrator
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = @User_RahulPatil OR [Email] = N'rahul.patil@technova.com')
BEGIN
    INSERT INTO [Users] (
        [Id], [FullName], [Email], [PasswordHash], [Role], [TenantId],
        [IsActive], [ProfileImageUrl], [CreatedAt], [LastLogin],
        [FailedLoginAttempts], [IsDeleted]
    )
    VALUES (
        @User_RahulPatil,
        N'Rahul Patil',
        N'rahul.patil@technova.com',
        @PasswordHash,
        N'TenantAdmin',
        @TenantId,
        1,
        N'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150',
        DATEADD(DAY, -30, @Now),
        DATEADD(HOUR, -2, @Now),
        0,
        0
    );
    PRINT 'Inserted User: Rahul Patil (TenantAdmin)';
END;

-- 2.2 Sneha More - Project Manager
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = @User_SnehaMore OR [Email] = N'sneha.more@technova.com')
BEGIN
    INSERT INTO [Users] (
        [Id], [FullName], [Email], [PasswordHash], [Role], [TenantId],
        [IsActive], [ProfileImageUrl], [CreatedAt], [LastLogin],
        [FailedLoginAttempts], [IsDeleted]
    )
    VALUES (
        @User_SnehaMore,
        N'Sneha More',
        N'sneha.more@technova.com',
        @PasswordHash,
        N'Manager',
        @TenantId,
        1,
        N'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150',
        DATEADD(DAY, -28, @Now),
        DATEADD(HOUR, -5, @Now),
        0,
        0
    );
    PRINT 'Inserted User: Sneha More (Manager)';
END;

-- 2.3 Akash Jadhav - Backend Member
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = @User_AkashJadhav OR [Email] = N'akash.jadhav@technova.com')
BEGIN
    INSERT INTO [Users] (
        [Id], [FullName], [Email], [PasswordHash], [Role], [TenantId],
        [IsActive], [ProfileImageUrl], [CreatedAt], [LastLogin],
        [FailedLoginAttempts], [IsDeleted]
    )
    VALUES (
        @User_AkashJadhav,
        N'Akash Jadhav',
        N'akash.jadhav@technova.com',
        @PasswordHash,
        N'Member',
        @TenantId,
        1,
        N'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150',
        DATEADD(DAY, -25, @Now),
        DATEADD(DAY, -1, @Now),
        0,
        0
    );
    PRINT 'Inserted User: Akash Jadhav (Member)';
END;

-- 2.4 Neha Kulkarni - Frontend Member
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = @User_NehaKulkarni OR [Email] = N'neha.kulkarni@technova.com')
BEGIN
    INSERT INTO [Users] (
        [Id], [FullName], [Email], [PasswordHash], [Role], [TenantId],
        [IsActive], [ProfileImageUrl], [CreatedAt], [LastLogin],
        [FailedLoginAttempts], [IsDeleted]
    )
    VALUES (
        @User_NehaKulkarni,
        N'Neha Kulkarni',
        N'neha.kulkarni@technova.com',
        @PasswordHash,
        N'Member',
        @TenantId,
        1,
        N'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150',
        DATEADD(DAY, -24, @Now),
        DATEADD(HOUR, -8, @Now),
        0,
        0
    );
    PRINT 'Inserted User: Neha Kulkarni (Member)';
END;

-- 2.5 Rohan Shah - QA / Testing Member
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = @User_RohanShah OR [Email] = N'rohan.shah@technova.com')
BEGIN
    INSERT INTO [Users] (
        [Id], [FullName], [Email], [PasswordHash], [Role], [TenantId],
        [IsActive], [ProfileImageUrl], [CreatedAt], [LastLogin],
        [FailedLoginAttempts], [IsDeleted]
    )
    VALUES (
        @User_RohanShah,
        N'Rohan Shah',
        N'rohan.shah@technova.com',
        @PasswordHash,
        N'Member',
        @TenantId,
        1,
        N'https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=150',
        DATEADD(DAY, -20, @Now),
        DATEADD(DAY, -2, @Now),
        0,
        0
    );
    PRINT 'Inserted User: Rohan Shah (Member)';
END;

-- -----------------------------------------------------------------------------
-- 3. Projects (3 Realistic Software Projects)
-- -----------------------------------------------------------------------------

-- 3.1 Employee Management System
IF NOT EXISTS (SELECT 1 FROM [Projects] WHERE [Id] = @Proj_EmployeeMgmt)
BEGIN
    INSERT INTO [Projects] (
        [Id], [Name], [Description], [TenantId], [OwnerId],
        [Status], [Priority], [StartDate], [EndDate],
        [IsActive], [IsDeleted], [CreatedAt]
    )
    VALUES (
        @Proj_EmployeeMgmt,
        N'Employee Management System',
        N'Internal HR portal with employee profiles, attendance logs, leave approvals, and monthly salary slip distribution.',
        @TenantId,
        @User_RahulPatil,
        N'In Progress',
        N'High',
        DATEADD(DAY, -20, @Now),
        DATEADD(DAY, 40, @Now),
        1,
        0,
        DATEADD(DAY, -20, @Now)
    );
    PRINT 'Inserted Project: Employee Management System';
END;

-- 3.2 Online Booking System
IF NOT EXISTS (SELECT 1 FROM [Projects] WHERE [Id] = @Proj_BookingSystem)
BEGIN
    INSERT INTO [Projects] (
        [Id], [Name], [Description], [TenantId], [OwnerId],
        [Status], [Priority], [StartDate], [EndDate],
        [IsActive], [IsDeleted], [CreatedAt]
    )
    VALUES (
        @Proj_BookingSystem,
        N'Online Booking System',
        N'Customer-facing appointment scheduling and calendar management platform with real-time slot verification.',
        @TenantId,
        @User_SnehaMore,
        N'In Progress',
        N'Medium',
        DATEADD(DAY, -15, @Now),
        DATEADD(DAY, 45, @Now),
        1,
        0,
        DATEADD(DAY, -15, @Now)
    );
    PRINT 'Inserted Project: Online Booking System';
END;

-- 3.3 Inventory Management
IF NOT EXISTS (SELECT 1 FROM [Projects] WHERE [Id] = @Proj_Inventory)
BEGIN
    INSERT INTO [Projects] (
        [Id], [Name], [Description], [TenantId], [OwnerId],
        [Status], [Priority], [StartDate], [EndDate],
        [IsActive], [IsDeleted], [CreatedAt]
    )
    VALUES (
        @Proj_Inventory,
        N'Inventory Management',
        N'Warehouse stock monitoring system with automated reorder triggers, supplier records, and inventory audit logs.',
        @TenantId,
        @User_RahulPatil,
        N'Completed',
        N'High',
        DATEADD(DAY, -60, @Now),
        DATEADD(DAY, -5, @Now),
        1,
        0,
        DATEADD(DAY, -60, @Now)
    );
    PRINT 'Inserted Project: Inventory Management';
END;

-- -----------------------------------------------------------------------------
-- 4. Project Members (Assign Users to Projects with Unique [ProjectId, UserId])
-- -----------------------------------------------------------------------------

-- Project 1 Members (Employee Management System)
IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_EmployeeMgmt AND [UserId] = @User_RahulPatil)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000001', @Proj_EmployeeMgmt, @User_RahulPatil);

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_EmployeeMgmt AND [UserId] = @User_SnehaMore)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000002', @Proj_EmployeeMgmt, @User_SnehaMore);

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_EmployeeMgmt AND [UserId] = @User_AkashJadhav)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000003', @Proj_EmployeeMgmt, @User_AkashJadhav);

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_EmployeeMgmt AND [UserId] = @User_NehaKulkarni)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000004', @Proj_EmployeeMgmt, @User_NehaKulkarni);

-- Project 2 Members (Online Booking System)
IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_BookingSystem AND [UserId] = @User_SnehaMore)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000005', @Proj_BookingSystem, @User_SnehaMore);

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_BookingSystem AND [UserId] = @User_RohanShah)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000006', @Proj_BookingSystem, @User_RohanShah);

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_BookingSystem AND [UserId] = @User_NehaKulkarni)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000007', @Proj_BookingSystem, @User_NehaKulkarni);

-- Project 3 Members (Inventory Management)
IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_Inventory AND [UserId] = @User_RahulPatil)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000008', @Proj_Inventory, @User_RahulPatil);

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_Inventory AND [UserId] = @User_AkashJadhav)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000009', @Proj_Inventory, @User_AkashJadhav);

IF NOT EXISTS (SELECT 1 FROM [ProjectMembers] WHERE [ProjectId] = @Proj_Inventory AND [UserId] = @User_RohanShah)
    INSERT INTO [ProjectMembers] ([Id], [ProjectId], [UserId]) VALUES ('44444444-aaaa-bbbb-cccc-000000000010', @Proj_Inventory, @User_RohanShah);

PRINT 'Inserted 10 Project Member mappings';

-- -----------------------------------------------------------------------------
-- 5. Tasks (12 Tasks Distributed Across To Do, In Progress, Completed)
-- -----------------------------------------------------------------------------

-- --- COLUMN 1: TO DO (4 tasks) ---
IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000001')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000001', N'Create login API', N'Develop authentication endpoint with JWT validation, refresh tokens, and rate limiting.', @Proj_EmployeeMgmt, @User_AkashJadhav, N'To Do', N'High', DATEADD(DAY, 5, @Now), 0, NULL, 0, DATEADD(DAY, -4, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000002')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000002', N'Add project validation', N'Ensure dates, budget fields, and team member assignments are strictly validated before persistence.', @Proj_EmployeeMgmt, @User_NehaKulkarni, N'To Do', N'Medium', DATEADD(DAY, 7, @Now), 0, NULL, 0, DATEADD(DAY, -3, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000003')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000003', N'Configure SMS booking alerts', N'Integrate SMS notification gateway to notify customers when an appointment is confirmed.', @Proj_BookingSystem, @User_RohanShah, N'To Do', N'Medium', DATEADD(DAY, 10, @Now), 0, NULL, 0, DATEADD(DAY, -2, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000004')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000004', N'Test user registration', N'Execute end-to-end regression tests on user onboarding, email verification, and password validation.', @Proj_BookingSystem, @User_RohanShah, N'To Do', N'Low', DATEADD(DAY, 12, @Now), 0, NULL, 0, DATEADD(DAY, -1, @Now), @TenantId);

-- --- COLUMN 2: IN PROGRESS (4 tasks) ---
IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000005')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000005', N'Create dashboard UI', N'Build responsive Angular analytics dashboard cards showing team attendance and monthly project velocity.', @Proj_EmployeeMgmt, @User_NehaKulkarni, N'In Progress', N'High', DATEADD(DAY, 3, @Now), 0, NULL, 0, DATEADD(DAY, -5, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000006')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000006', N'Design booking calendar view', N'Implement responsive calendar grid supporting month, week, and day views for booking appointments.', @Proj_BookingSystem, @User_NehaKulkarni, N'In Progress', N'Medium', DATEADD(DAY, 6, @Now), 0, NULL, 0, DATEADD(DAY, -6, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000007')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000007', N'Fix task status update', N'Resolve optimistic locking issue when moving tasks quickly across the Kanban board.', @Proj_EmployeeMgmt, @User_AkashJadhav, N'In Progress', N'High', DATEADD(DAY, 2, @Now), 0, NULL, 0, DATEADD(DAY, -2, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000008')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000008', N'Implement slot reservation API', N'Write high-concurrency lock-protected backend endpoints to prevent double-booking of time slots.', @Proj_BookingSystem, @User_SnehaMore, N'In Progress', N'High', DATEADD(DAY, 4, @Now), 0, NULL, 0, DATEADD(DAY, -7, @Now), @TenantId);

-- --- COLUMN 3: COMPLETED (4 tasks) ---
IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000009')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000009', N'Design user table', N'Create EF Core user entity configuration with indexes on Email, TenantId, and soft-delete flags.', @Proj_EmployeeMgmt, @User_AkashJadhav, N'Completed', N'High', DATEADD(DAY, -10, @Now), 1, DATEADD(DAY, -10, @Now), 0, DATEADD(DAY, -18, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000010')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000010', N'Setup database migrations', N'Run initial Code-First migrations and verify SQL foreign key constraints.', @Proj_EmployeeMgmt, @User_RahulPatil, N'Completed', N'High', DATEADD(DAY, -15, @Now), 1, DATEADD(DAY, -15, @Now), 0, DATEADD(DAY, -20, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000011')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000011', N'Stock reorder notification logic', N'Automated background calculation comparing current warehouse inventory with min-safety thresholds.', @Proj_Inventory, @User_AkashJadhav, N'Completed', N'Medium', DATEADD(DAY, -8, @Now), 1, DATEADD(DAY, -8, @Now), 0, DATEADD(DAY, -30, @Now), @TenantId);

IF NOT EXISTS (SELECT 1 FROM [TaskItems] WHERE [Id] = '55555555-aaaa-bbbb-cccc-000000000012')
    INSERT INTO [TaskItems] ([Id], [Name], [Description], [ProjectId], [AssignedUserId], [Status], [Priority], [DueDate], [IsCompleted], [CompletedAt], [IsDeleted], [CreatedAt], [TenantId])
    VALUES ('55555555-aaaa-bbbb-cccc-000000000012', N'Vendor invoice export to Excel', N'Generate formatted Excel workbooks with stock items, vendor details, and payment statuses via EPPlus.', @Proj_Inventory, @User_RohanShah, N'Completed', N'Low', DATEADD(DAY, -6, @Now), 1, DATEADD(DAY, -6, @Now), 0, DATEADD(DAY, -25, @Now), @TenantId);

PRINT 'Inserted 12 Workspace Tasks';

-- -----------------------------------------------------------------------------
-- 6. Payments (Realistic Development Subscription Billing History)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [Payments] WHERE [Id] = '66666666-aaaa-bbbb-cccc-000000000001')
BEGIN
    INSERT INTO [Payments] (
        [Id], [UserId], [TenantId], [SubscriptionPlanId],
        [Amount], [PaymentMethod], [PaymentStatus],
        [TransactionId], [PaymentDate]
    )
    VALUES (
        '66666666-aaaa-bbbb-cccc-000000000001',
        @User_RahulPatil,
        @TenantId,
        @ProPlanId,
        45.00,
        N'UPI',
        N'Success',
        N'UPI-TXN-20260826-89412',
        DATEADD(DAY, -30, @Now)
    );
END;

IF NOT EXISTS (SELECT 1 FROM [Payments] WHERE [Id] = '66666666-aaaa-bbbb-cccc-000000000002')
BEGIN
    INSERT INTO [Payments] (
        [Id], [UserId], [TenantId], [SubscriptionPlanId],
        [Amount], [PaymentMethod], [PaymentStatus],
        [TransactionId], [PaymentDate]
    )
    VALUES (
        '66666666-aaaa-bbbb-cccc-000000000002',
        @User_RahulPatil,
        @TenantId,
        @ProPlanId,
        45.00,
        N'Card',
        N'Success',
        N'CARD-TXN-20260925-54219',
        DATEADD(DAY, -1, @Now)
    );
END;

PRINT 'Inserted 2 Payment Billing Records';

-- -----------------------------------------------------------------------------
-- 7. Notifications (4 Realistic Workspace Notifications)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [Notifications] WHERE [Id] = '77777777-aaaa-bbbb-cccc-000000000001')
    INSERT INTO [Notifications] ([Id], [TenantId], [Message], [IsRead], [CreatedAt])
    VALUES ('77777777-aaaa-bbbb-cccc-000000000001', @TenantId, N'Welcome to TechNova Solutions workspace! Your Pro subscription is now active.', 1, DATEADD(DAY, -30, @Now));

IF NOT EXISTS (SELECT 1 FROM [Notifications] WHERE [Id] = '77777777-aaaa-bbbb-cccc-000000000002')
    INSERT INTO [Notifications] ([Id], [TenantId], [Message], [IsRead], [CreatedAt])
    VALUES ('77777777-aaaa-bbbb-cccc-000000000002', @TenantId, N'Project ''Employee Management System'' was initialized by Rahul Patil.', 1, DATEADD(DAY, -20, @Now));

IF NOT EXISTS (SELECT 1 FROM [Notifications] WHERE [Id] = '77777777-aaaa-bbbb-cccc-000000000003')
    INSERT INTO [Notifications] ([Id], [TenantId], [Message], [IsRead], [CreatedAt])
    VALUES ('77777777-aaaa-bbbb-cccc-000000000003', @TenantId, N'Akash Jadhav assigned you a new task: ''Create dashboard UI''.', 0, DATEADD(DAY, -3, @Now));

IF NOT EXISTS (SELECT 1 FROM [Notifications] WHERE [Id] = '77777777-aaaa-bbbb-cccc-000000000004')
    INSERT INTO [Notifications] ([Id], [TenantId], [Message], [IsRead], [CreatedAt])
    VALUES ('77777777-aaaa-bbbb-cccc-000000000004', @TenantId, N'Monthly subscription payment of $45.00 was successfully processed via Card.', 0, DATEADD(DAY, -1, @Now));

PRINT 'Inserted 4 Workspace Notifications';

-- -----------------------------------------------------------------------------
-- 8. Reports (2 Development Export Records)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [Reports] WHERE [Id] = '88888888-aaaa-bbbb-cccc-000000000001')
    INSERT INTO [Reports] ([Id], [TenantId], [ReportType], [GeneratedByUserId], [FilePath], [GeneratedAt])
    VALUES ('88888888-aaaa-bbbb-cccc-000000000001', @TenantId, N'PDF - Monthly Project Status Report', @User_RahulPatil, N'wwwroot/uploads/reports/technova_monthly_status.pdf', DATEADD(DAY, -5, @Now));

IF NOT EXISTS (SELECT 1 FROM [Reports] WHERE [Id] = '88888888-aaaa-bbbb-cccc-000000000002')
    INSERT INTO [Reports] ([Id], [TenantId], [ReportType], [GeneratedByUserId], [FilePath], [GeneratedAt])
    VALUES ('88888888-aaaa-bbbb-cccc-000000000002', @TenantId, N'Excel - Team Velocity & Task Completion Summary', @User_SnehaMore, N'wwwroot/uploads/reports/technova_team_summary.xlsx', DATEADD(DAY, -2, @Now));

PRINT 'Inserted 2 Report Records';

-- -----------------------------------------------------------------------------
-- 9. System / Audit Logs (5 Realistic Security & Activity Logs)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [SystemLogs] WHERE [Id] = '99999999-aaaa-bbbb-cccc-000000000001')
    INSERT INTO [SystemLogs] ([Id], [Action], [Description], [UserId], [TenantId], [CreatedAt])
    VALUES ('99999999-aaaa-bbbb-cccc-000000000001', N'TENANT_REGISTERED', N'Organization TechNova Solutions onboarded on Pro Plan.', @User_RahulPatil, @TenantId, DATEADD(DAY, -30, @Now));

IF NOT EXISTS (SELECT 1 FROM [SystemLogs] WHERE [Id] = '99999999-aaaa-bbbb-cccc-000000000002')
    INSERT INTO [SystemLogs] ([Id], [Action], [Description], [UserId], [TenantId], [CreatedAt])
    VALUES ('99999999-aaaa-bbbb-cccc-000000000002', N'USER_INVITED', N'Sneha More invited as Manager by Rahul Patil.', @User_RahulPatil, @TenantId, DATEADD(DAY, -28, @Now));

IF NOT EXISTS (SELECT 1 FROM [SystemLogs] WHERE [Id] = '99999999-aaaa-bbbb-cccc-000000000003')
    INSERT INTO [SystemLogs] ([Id], [Action], [Description], [UserId], [TenantId], [CreatedAt])
    VALUES ('99999999-aaaa-bbbb-cccc-000000000003', N'PROJECT_CREATED', N'Project ''Employee Management System'' created by Rahul Patil.', @User_RahulPatil, @TenantId, DATEADD(DAY, -20, @Now));

IF NOT EXISTS (SELECT 1 FROM [SystemLogs] WHERE [Id] = '99999999-aaaa-bbbb-cccc-000000000004')
    INSERT INTO [SystemLogs] ([Id], [Action], [Description], [UserId], [TenantId], [CreatedAt])
    VALUES ('99999999-aaaa-bbbb-cccc-000000000004', N'TASK_STATUS_CHANGED', N'Task ''Design user table'' moved to Completed by Akash Jadhav.', @User_AkashJadhav, @TenantId, DATEADD(DAY, -10, @Now));

IF NOT EXISTS (SELECT 1 FROM [SystemLogs] WHERE [Id] = '99999999-aaaa-bbbb-cccc-000000000005')
    INSERT INTO [SystemLogs] ([Id], [Action], [Description], [UserId], [TenantId], [CreatedAt])
    VALUES ('99999999-aaaa-bbbb-cccc-000000000005', N'USER_LOGIN', N'User rahul.patil@technova.com logged in successfully.', @User_RahulPatil, @TenantId, DATEADD(HOUR, -2, @Now));

PRINT 'Inserted 5 System Logs';

COMMIT TRANSACTION;
PRINT '====================================================================';
PRINT 'DevelopmentData.sql executed successfully! All records seeded.';
PRINT '====================================================================';
