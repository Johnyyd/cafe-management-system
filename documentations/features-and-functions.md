# Cafe Management System - Features and Functions

This document outlines the features of the system and the corresponding functions (API endpoints) that implement them.

## API Design Standards

### Versioning
All endpoints are versioned under `/api/v1/` (e.g., `/api/v1/shops`).

### Pagination
**Cursor-based pagination** is used for all list endpoints:
- Query parameters: `after` (cursor for next page), `before` (cursor for previous page), `limit` (max items, default 20, max 100)
- Response includes: `data[]`, `pagination: { nextCursor, previousCursor, hasMore }`
- Cursors are opaque base64-encoded tokens containing sort key and unique identifier

### Error Responses
All errors follow **RFC 7807 Problem Details** format:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors property for details.",
  "instance": "/api/v1/shops",
  "errors": {
    "name": ["The Name field is required."]
  }
}
```
Common HTTP status codes:
- 200 OK - Success
- 201 Created - Resource created
- 400 Bad Request - Validation error (RFC 7807)
- 401 Unauthorized - Missing/invalid token
- 403 Forbidden - Insufficient permissions
- 404 Not Found - Resource not found
- 409 Conflict - Business rule violation
- 422 Unprocessable Entity - Semantic error
- 429 Too Many Requests - Rate limited
- 500 Internal Server Error - Unexpected error

### Authentication
- JWT Bearer tokens via Authorization header
- Tokens issued by ASP.NET Core Identity
- Refresh token rotation for security

### Idempotency
POST and PUT endpoints support `Idempotency-Key` header for safe retries.

---

## 1. Authentication & Authorization

### Features
- User registration (for initial admin setup)
- Login/logout functionality
- JWT-based authentication
- Role-based access control (RBAC)
- Password hashing and security
- Token refresh mechanism

### Functions (API Endpoints)
- `POST /api/v1/auth/register` - Register a new user (admin only for production)
- `POST /api/v1/auth/login` - Authenticate user and return JWT token
- `POST /api/v1/auth/logout` - Invalidate token (client-side, but endpoint for audit)
- `POST /api/v1/auth/refresh-token` - Refresh expired access token using refresh token
- `GET /api/v1/auth/me` - Get current user's profile and permissions
- `PUT /api/v1/auth/change-password` - Change password (requires old password)
- `POST /api/v1/auth/forgot-password` - Initiate password reset
- `POST /api/v1/auth/reset-password` - Reset password with token

## 2. Shop Management

### Features
- Create, read, update, delete shop information
- Manage shop operating hours
- Activate/deactivate shops (under maintenance, closed)
- View shop status and details

### Functions (API Endpoints)
- `GET /api/v1/shops` - List all shops (with pagination, filtering)
- `GET /api/v1/shops/{id}` - Get shop by ID
- `POST /api/v1/shops` - Create a new shop
- `PUT /api/v1/shops/{id}` - Update shop information
- `DELETE /api/v1/shops/{id}` - Deactivate shop (soft delete) or set status
- `GET /api/v1/shops/{id}/hours` - Get operating hours for a shop
- `PUT /api/v1/shops/{id}/hours` - Update operating hours

## 3. Staff Management

### Features
- Staff CRUD operations
- Assign roles and permissions
- Manage employment status
- View staff profile and assignment history
- Filter staff by shop, role, status

### Functions (API Endpoints)
- `GET /api/v1/staff` - List staff (with pagination, filtering by shopId, role, status)
- `GET /api/v1/staff/{id}` - Get staff by ID
- `POST /api/v1/staff` - Create new staff member
- `PUT /api/v1/staff/{id}` - Update staff information
- `DELETE /api/v1/staff/{id}` - Terminate staff (soft delete or change status)
- `GET /api/v1/staff/{id}/shifts` - Get shift history for a staff member
- `PUT /api/v1/staff/{id}/role` - Update staff role

## 4. Inventory Management

### Features
- Track inventory items (ingredients, supplies)
- Set reorder levels and receive low stock alerts
- Record inventory usage (linked to orders or manual adjustment)
- Manage supplier information
- View inventory valuation and reports
- Adjust inventory quantities with reason tracking
- Set inventory quantities directly
- Get low stock and out of stock items
- Get inventory item names by shop
- Get inventory item by shop and item name

### Functions (API Endpoints)
- `GET /api/v1/inventory` - List inventory items (with pagination, filtering by shopId)
- `GET /api/v1/inventory/{id}` - Get inventory item by ID
- `POST /api/v1/inventory` - Add new inventory item
- `PUT /api/v1/inventory/{id}` - Update inventory item
- `DELETE /api/v1/inventory/{id}` - Deactivate inventory item
- `POST /api/v1/inventory/{id}/adjust-quantity` - Adjust inventory quantity (increase/decrease with reason)
- `POST /api/v1/inventory/{id}/set-quantity` - Set inventory quantity directly
- `GET /api/v1/inventory/low-stock` - Get low stock inventory items
- `GET /api/v1/inventory/out-of-stock` - Get out of stock inventory items
- `GET /api/v1/inventory/item-names` - Get inventory item names (with shopId filter)
- `GET /api/v1/inventory/{shopId}/{itemName}` - Get inventory item by shop ID and item name
- `POST /api/v1/inventory/{id}/deactivate` - Deactivate inventory item (alternative endpoint)
- `GET /api/v1/suppliers` - List suppliers
- `GET /api/v1/suppliers/{id}` - Get supplier by ID
- `POST /api/v1/suppliers` - Add new supplier
- `PUT /api/v1/suppliers/{id}` - Update supplier information
- `DELETE /api/v1/suppliers/{id}` - Deactivate supplier

## 5. Menu Management

### Features
- Manage menu items (categories, names, descriptions, pricing)
- Set item availability (time of day, days of week)
- Mark items as seasonal or unavailable
- Manage ingredients and allergens information
- View menu performance (via reports)

### Functions (API Endpoints)
- `GET /api/v1/menu` - List menu items (with pagination, filtering by shopId, category, availability)
- `GET /api/v1/menu/{id}` - Get menu item by ID
- `POST /api/v1/menu` - Create new menu item
- `PUT /api/v1/menu/{id}` - Update menu item
- `DELETE /api/v1/menu/{id}` - Deactivate menu item
- `GET /api/v1/menu/categories` - List all unique categories
- `POST /api/v1/menu/{id}/toggle-availability` - Toggle item availability for specific time/day

## 6. Order Management

### Features
- Create orders (dine-in, takeaway, online)
- Modify orders before payment
- Cancel orders
- Track order status (preparing, ready, completed)
- Process payments (integrated with payment gateway - stubbed)
- Generate receipts
- View order history

### Functions (API Endpoints)
- `GET /api/v1/orders` - List orders (with pagination, filtering by shopId, status, date range)
- `GET /api/v1/orders/{id}` - Get order by ID
- `POST /api/v1/orders` - Create a new order
- `PUT /api/v1/orders/{id}` - Update order (e.g., add items, change special instructions - only if status allows)
- `DELETE /api/v1/orders/{id}` - Cancel order (if not yet paid/completed)
- `POST /api/v1/orders/{id}/payment` - Process payment for order
- `GET /api/v1/orders/{id}/receipt` - Generate receipt (PDF or text)
- `PUT /api/v1/orders/{id}/status` - Update order status (e.g., from preparing to ready)

## 7. Quality Assurance (QA)

### Features
- Define QA checklists for service and product
- Perform QA inspections and record results
- Track QA scores over time
- Capture customer feedback and ratings
- Identify areas for improvement

### Functions (API Endpoints)
#### QA Checklists
- `GET /api/v1/qa/checklists` - List checklists (with filtering by shopId, type, active)
- `GET /api/v1/qa/checklists/{id}` - Get checklist by ID
- `POST /api/v1/qa/checklists` - Create new checklist
- `PUT /api/v1/qa/checklists/{id}` - Update checklist
- `DELETE /api/v1/qa/checklists/{id}` - Deactivate checklist

#### QA Records
- `GET /api/v1/qa/records` - List QA records (with pagination, filtering by shopId, checklistId, date range)
- `GET /api/v1/qa/records/{id}` - Get QA record by ID
- `POST /api/v1/qa/records` - Create new QA record (perform an inspection)
- `PUT /api/v1/qa/records/{id}` - Update QA record (e.g., add notes)

#### Customer Feedback
- `GET /api/v1/feedback` - List customer feedback (with pagination, filtering by shopId, date range, rating)
- `GET /api/v1/feedback/{id}` - Get feedback by ID
- `POST /api/v1/feedback` - Submit new customer feedback
- `PUT /api/v1/feedback/{id}/respond` - Add management response to feedback (optional)

## 8. Reporting & Analytics

### Features
- Sales reports (daily, weekly, monthly, custom range)
- Inventory usage reports
- Staff performance metrics (orders served, QA scores)
- QA trend analysis
- Dashboard with key metrics

### Functions (API Endpoints)
- `GET /api/v1/reports/sales` - Get sales report (parameters: shopId, startDate, endDate, groupBy: day/week/month)
- `GET /api/v1/reports/inventory` - Get inventory usage report (parameters: shopId, startDate, endDate)
- `GET /api/v1/reports/staff-performance` - Get staff performance report (parameters: shopId, startDate, endDate)
- `GET /api/v1/reports/qa-trends` - Get QA scores trend over time (parameters: shopId, checklistId, startDate, endDate)
- `GET /api/v1/dashboard/summary` - Get dashboard summary for a shop or chain (parameters: shopId optional for chain-wide)

## 9. Settings & Configuration

### Features
- System-wide settings (tax rates, currency, date/time formats)
- Notification preferences (email/SMS thresholds)
- Audit log viewing
- Backup and restore (initiated by admin)

### Functions (API Endpoints)
- `GET /api/v1/settings` - Get current system settings
- `PUT /api/v1/settings` - Update system settings (admin only)
- `GET /api/v1/settings/notifications` - Get notification settings
- `PUT /api/v1/settings/notifications` - Update notification settings
- `GET /api/v1/audit-logs` - View audit logs (admin only, with pagination and filtering)
- `POST /api/v1/backup` - Initiate backup (admin only)
- `POST /api/v1/restore` - Initiate restore from backup (admin only)

## 10. Shift Management

### Features
- Create and manage work shifts
- Assign staff to shifts
- Track shift attendance and breaks
- View shift history and reports

### Functions (API Endpoints)
- `GET /api/v1/shifts` - List shifts (with pagination, filtering by shopId, staffId, date range)
- `GET /api/v1/shifts/{id}` - Get shift by ID
- `POST /api/v1/shifts` - Create new shift
- `PUT /api/v1/shifts/{id}` - Update shift (e.g., end time, notes)
- `DELETE /api/v1/shifts/{id}` - Delete shift (if not started)
- `POST /api/v1/shifts/{id}/clock-in` - Staff clocks in for shift
- `POST /api/v1/shifts/{id}/clock-out` - Staff clocks out from shift
- `POST /api/v1/shifts/{id}/break-start` - Start break
- `POST /api/v1/shifts/{id}/break-end` - End break

## Notes on API Design
- All endpoints are prefixed with `/api/v1/` (versioned).
- Data transfer objects (DTOs) are used to shape requests and responses.
- Error handling returns standard HTTP status codes and **RFC 7807 Problem Details** format.
- Authentication middleware validates JWT tokens for protected endpoints.
- Role-based authorization is enforced at the endpoint or service level.
- **Pagination uses cursor-based approach** (`after`, `before`, `limit` parameters).
- Filtering, sorting, and searching are supported via query parameters where applicable.
- Dates are transmitted in ISO 8601 format.
- Decimal values for money are represented as **Decimal128** in MongoDB.
- **Idempotency supported via `Idempotency-Key` header** for POST/PUT endpoints.
- **Rate limiting** applied to all endpoints (configurable per endpoint).

