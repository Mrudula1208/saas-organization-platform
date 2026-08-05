# API Login Issue Fix Documentation

## 1. Issue: User Login Failure (ArgumentNullException during Token Generation)

### Why it was occurring:
When a user logged in, the `UserService.LoginAsync` method generated a JWT token and attempted to include user-specific claims such as `Role` and `Email`. However, if the `User.Role` or `User.Email` fields in the database were `null`, the `Claim` constructor threw an `ArgumentNullException`. Claims do not accept `null` values.
Additionally, the token's expiration date was being calculated using `DateTime.Now`, which uses the server's local time instead of the universally recommended `DateTime.UtcNow`.

### How it was solved:
We modified the `LoginAsync` method in `UserService.cs` to handle potentially null values using the null-coalescing operator (`??`). 
- `user.Role ?? "User"` ensures that if a role is null, it defaults to `"User"`.
- `user.Email ?? string.Empty` ensures that an empty string is passed instead of `null`.
- We updated the expiration time from `DateTime.Now.AddHours(2)` to `DateTime.UtcNow.AddHours(2)` to prevent timezone-related token validation issues.

---

## 2. Issue: Entity Framework Core Tracking Error during User Update

### Why it was occurring:
In `UserRepository.cs`, the `UpdateUser` method was executing the following sequence:
1. `_context.Users.Update(user);`
2. `await _context.SaveChangesAsync();`
3. Then immediately fetching the same user again: `var existingUser = await _context.Users.FindAsync(Id);`
4. Manually updating `FullName` and `Email`.
5. Finally calling `await _context.SaveChangesAsync();` again.

This "double update" is anti-pattern and highly dangerous in EF Core. Since the `user` entity was already being tracked by `GetByEmailAsync` before `UpdateUser` was even called, explicitly calling `_context.Users.Update(user)` threw tracking exceptions. Moreover, multiple `SaveChangesAsync()` calls in quick succession degraded performance.

### How it was solved:
We refactored `UpdateUser` in `UserRepository.cs` to cleanly check for existing entities and update their values using `CurrentValues.SetValues(user)`. We also ensure that we do not try to update an entity if it's the exact same tracked instance in memory (using `!ReferenceEquals(existingUser, user)`). This resolves any internal tracking errors and reduces database roundtrips.

---

## Summary of Modified Files
1. **`backend\SaaSPlatform_DataAccess\Services\UserService.cs`** 
   - Fixed null claim exception for `Role` and `Email`.
   - Changed Token Expiration to use UTC Time.
2. **`backend\SaaSOrganizationPlatform\SaaSPlatform.Infrastructure\Repositories\UserRepository.cs`**
   - Removed redundant double-save.
   - Refactored `UpdateUser` to prevent Entity Framework entity tracking collisions.
