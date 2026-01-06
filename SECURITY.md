# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in MeetApp, please report it by creating a private security advisory on GitHub or by contacting the repository owner directly. **Do not** create a public issue for security vulnerabilities.

## Security Measures Implemented

### Authentication & Authorization
- **JWT Bearer Token Authentication**: Secure token-based authentication
- **Strong Password Requirements**: Enforced 8+ characters with uppercase, lowercase, digit, and special character
- **Role-based Authorization**: Student and Business user roles
- **User Profile Protection**: Users can only update their own profiles (enforced at API level)

### Data Protection
- **Environment-based Logging**: Sensitive data logging and detailed errors only enabled in development mode
- **Parameterized Queries**: SQL injection protection via Entity Framework Core parameterization
- **CORS Policy**: Restricted to specific allowed origins
- **Configuration Security**: Secrets stored in environment variables, not in code

### API Security
- **Input Validation**: Model validation on all API endpoints
- **Authorization Checks**: Proper authorization on sensitive endpoints
- **HTTPS Support**: Available for production deployments
- **Rate Limiting**: Consider implementing for production

## Security Fixes Applied

### Version: Current
- **Fixed**: User update endpoints now require authentication and authorization
  - Users can only update their own profiles
  - Added `[Authorize]` attribute and user ID validation
- **Fixed**: Sensitive data logging only enabled in development mode
  - `EnableDetailedErrors()` and `EnableSensitiveDataLogging()` conditional on environment
- **Fixed**: JWT token expiry configuration with safe defaults
  - Defaults to 60 minutes if not configured

## Security Best Practices for Deployment

### Configuration
1. **Use Strong JWT Secrets**: Minimum 32 characters, randomly generated
2. **Store Secrets Securely**: Use environment variables, Azure Key Vault, or similar
3. **Enable HTTPS**: Always use HTTPS in production
4. **Configure CORS**: Update CORS policy with production domains only
5. **Update Connection Strings**: Use secure database credentials

### Database
1. **Use Strong Database Passwords**: Complex passwords with restricted access
2. **Enable SSL for Database**: Configure `TrustServerCertificate=False` with proper certificates
3. **Regular Backups**: Implement automated backup strategy
4. **Keep Schema Updated**: Run migrations in controlled manner

### Frontend
1. **Environment Variables**: Never commit `.env` files
2. **API Keys**: Use backend proxy for sensitive API calls when possible
3. **Content Security Policy**: Consider implementing CSP headers
4. **Dependency Updates**: Regularly update npm packages

### Infrastructure
1. **Keep Dependencies Updated**: Regularly update NuGet and npm packages
2. **Monitor Security Advisories**: Subscribe to security advisories for used packages
3. **Regular Security Scans**: Run automated security scans
4. **Limit Access**: Use principle of least privilege for all services

## Known Security Considerations

### Current Implementation Notes
1. **Azure Translation API**: Currently commented out - ensure proper key management when enabling
2. **Stripe Integration**: Currently commented out - follow PCI compliance when enabling
3. **Database Migrations**: Currently commented out in startup - consider migration strategy
4. **Session Management**: JWT tokens don't support revocation - consider implementing token blacklist for critical scenarios

### Recommended Enhancements
1. **Rate Limiting**: Implement API rate limiting to prevent abuse
2. **Account Lockout**: Implement account lockout after failed login attempts
3. **Audit Logging**: Add comprehensive audit logging for sensitive operations
4. **Two-Factor Authentication**: Consider implementing 2FA for enhanced security
5. **Email Verification**: Implement email verification for new registrations
6. **Password Reset**: Implement secure password reset functionality
7. **CSRF Protection**: Ensure CSRF protection for state-changing operations
8. **Input Sanitization**: Add additional input sanitization for user-generated content

## Dependency Security

### Regular Security Checks
Run these commands regularly to check for vulnerabilities:

```bash
# .NET dependencies
dotnet list package --vulnerable

# Frontend dependencies
cd src/MeetApp.Frontend
npm audit
```

### Update Policy
- **Critical Vulnerabilities**: Update immediately
- **High Vulnerabilities**: Update within 7 days
- **Medium/Low Vulnerabilities**: Update in next scheduled maintenance

## Security Contact

For security concerns, please contact the repository maintainer through GitHub.

---

**Last Updated**: 2026-01-06
