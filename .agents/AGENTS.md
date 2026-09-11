# Project Rules for SaaSOrganizationPlatform

## Feature Commit Monitoring
When helping build the Multi-Tenant SaaS Organization Platform, monitor the progress feature by feature.
When a meaningful unit of work (such as Authentication, Tenant Management, Project CRUD, Task Management, Reports, Dashboard, Billing, Notifications, etc.) is finished, stop and prompt the user to commit:
- State: "✅ It's time to commit."
- Give the exact `git add` commands.
- Provide a professional Conventional Commit message (following the Conventional Commits specification, e.g., `feat(auth): implement login and registration pages`).
- Explain why this is the correct place to commit.
- Advise on what feature to build next.
