# Cafe Management System - Features and Functions

This document outlines the features of the system and the corresponding functions (API endpoints) that implement them.

## 1. Authentication & Authorization

### Features
- User registration (for initial admin setup)
- Login/logout functionality
- JWT-based authentication
- Role-based access control (RBAC)
- Password hashing and security
- Token refresh mechanism

### Functions (API Endpoints)
- `POST /api/auth/register` - Register a new user (admin only for production)
- `POST /api/auth/login` - Authenticate user and return JWT token
- `POST /api/auth/logout` - Invalidate token (client-side, but endpoint for audit)
- `POST /api/auth/refresh-token` - Refresh expired access token using refresh token
- `GET /api/auth/me` - Get current user's profile and permissions
- `PUT /api/auth/change-password` - Change password (requires old password)
- `POST /api/auth/forgot-password` - Initiate password reset
- `POST /api/auth/reset-password` - Reset password with token

## 2. Shop Management

### Features
- Create, read, update, delete shop information
- Manage shop operating hours
- Activate/deactivate shops (under maintenance, closed)
- View shop status and details

### Functions (API Endpoints)
- `GET /api/shops` - List all shops (with pagination, filtering)
- `GET /api/shops/{id}` - Get shop by ID
- `POST /api/shops` - Create a new shop
- `PUT /api/shops/{id}` - Update shop information
- `DELETE /api/shops/{id}` - Deactivate shop (soft delete) or set status
- `GET /api/shops/{id}/hours` - Get operating hours for a shop
- `PUT /api/shops/{id}/hours` - Update operating hours

## 3. Staff Management

### Features
- Staff CRUD operations
- Assign roles and permissions
- Manage employment status
- View staff profile and assignment history
- Filter staff by shop, role, status

### Functions (API Endpoints)
- `GET /api/staff` - List staff (with pagination, filtering by shopId, role, status)
- `GET /api/staff/{id}` - Get staff by ID
- `POST /api/staff` - Create new staff member
- `PUT /api/staff/{id}` - Update staff information
- `DELETE /api/staff/{id}` - Terminate staff (soft delete or change status)
- `GET /api/staff/{id}/shifts` - Get shift history for a staff member
- `PUT /api/staff/{id}/role` - Update staff role

## 4. Inventory Management

### Features
- Track inventory items (ingredients, supplies)
- Set reorder levels and receive low stock alerts
- Record inventory usage (linked to orders or manual adjustment)
- Manage supplier information
- View inventory valuation and reports

### Functions (API Endpoints)
- `GET /api/inventory` - List inventory items (with pagination, filtering by shopId, low stock)
- `GET /api/inventory/{id}` - Get inventory item by ID
- `POST /api/inventory` - Add new inventory item
- `PUT /api/inventory/{id}` - Update inventory item (quantity, etc.)
- `DELETE /api/inventory/{id}` - Remove inventory item
- `POST /api/inventory/{id}/adjust` - Adjust inventory quantity (increase/decrease with reason)
- `GET /api/suppliers` - List suppliers
- `GET /api/suppliers/{id}` - Get supplier by ID
- `POST /api/suppliers` - Add new supplier
- `PUT /api/suppliers/{id}` - Update supplier information
- `DELETE /api/suppliers/{id}` - Deactivate supplier

## 5. Menu Management

### Features
- Manage menu items (categories, names, descriptions, pricing)
- Set item availability (time of day, days of week)
- Mark items as seasonal or unavailable
- Manage ingredients and allergens information
- View menu performance (via reports)

### Functions (API Endpoints)
- `GET /api/menu` - List menu items (with pagination, filtering by shopId, category, availability)
- `GET /api/menu/{id}` - Get menu item by ID
- `POST /api/menu` - Create new menu item
- `PUT /api/menu/{id}` - Update menu item
- `DELETE /api/menu/{id}` - Deactivate menu item
- `GET /api/menu/categories` - List all unique categories
- `POST /api/menu/{id}/toggle-availability` - Toggle item availability for specific time/day

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
- `GET /api/orders` - List orders (with pagination, filtering by shopId, status, date range)
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create a new order
- `PUT /api/orders/{id}` - Update order (e.g., add items, change special instructions - only if status allows)
- `DELETE /api/orders/{id}` - Cancel order (if not yet paid/completed)
- `POST /api/orders/{id}/payment` - Process payment for order
- `GET /api/orders/{id}/receipt` - Generate receipt (PDF or text)
- `PUT /api/orders/{id}/status` - Update order status (e.g., from preparing to ready)

## 7. Quality Assurance (QA)

### Features
- Define QA checklists for service and product
- Perform QA inspections and record results
- Track QA scores over time
- Capture customer feedback and ratings
- Identify areas for improvement

### Functions (API Endpoints)
#### QA Checklists
- `GET /api/qa/checklists` - List checklists (with filtering by shopId, type, active)
- `GET /api/qa/checklists/{id}` - Get checklist by ID
- `POST /api/qa/checklists` - Create new checklist
- `PUT /api/qa/checklists/{id}` - Update checklist
- `DELETE /api/qa/checklists/{id}` - Deactivate checklist

#### QA Records
- `GET /api/qa/records` - List QA records (with pagination, filtering by shopId, checklistId, date range)
- `GET /api/qa/records/{id}` - Get QA record by ID
- `POST /api/qa/records` - Create new QA record (perform an inspection)
- `PUT /api/qa/records/{id}` - Update QA record (e.g., add notes)

#### Customer Feedback
- `GET /api/feedback` - List customer feedback (with pagination, filtering by shopId, date range, rating)
- `GET /api/feedback/{id}` - Get feedback by ID
- `POST /api/feedback` - Submit new customer feedback
- `PUT /api/feedback/{id}/respond` - Add management response to feedback (optional)

## 8. Reporting & Analytics

### Features
- Sales reports (daily, weekly, monthly, custom range)
- Inventory usage reports
- Staff performance metrics (orders served, QA scores)
- QA trend analysis
- Dashboard with key metrics

### Functions (API Endpoints)
- `GET /api/reports/sales` - Get sales report (parameters: shopId, startDate, endDate, groupBy: day/week/month)
- `GET /api/reports/inventory` - Get inventory usage report (parameters: shopId, startDate, endDate)
- `GET /api/reports/staff-performance` - Get staff performance report (parameters: shopId, startDate, endDate)
- `GET /api/reports/qa-trends` - Get QA scores trend over time (parameters: shopId, checklistId, startDate, endDate)
- `GET /api/dashboard/summary` - Get dashboard summary for a shop or chain (parameters: shopId optional for chain-wide)

## 9. Settings & Configuration

### Features
- System-wide settings (tax rates, currency, date/time formats)
- Notification preferences (email/SMS thresholds)
- Audit log viewing
- Backup and restore (initiated by admin)

### Functions (API Endpoints)
- `GET /api/settings` - Get current system settings
- `PUT /api/settings` - Update system settings (admin only)
- `GET /api/settings/notifications` - Get notification settings
- `PUT /api/settings/notifications` - Update notification settings
- `GET /api/audit-logs` - View audit logs (admin only, with pagination and filtering)
- `POST /api/backup` - Initiate backup (admin only)
- `POST /api/restore` - Initiate restore from backup (admin only)

## 10. Shift Management

### Features
- Create and manage work shifts
- Assign staff to shifts
- Track shift attendance and breaks
- View shift history and reports

### Functions (API Endpoints)
- `GET /api/shifts` - List shifts (with pagination, filtering by shopId, staffId, date range)
- `GET /api/shifts/{id}` - Get shift by ID
- `POST /api/shifts` - Create new shift
- `PUT /api/shifts/{id}` - Update shift (e.g., end time, notes)
- `DELETE /api/shifts/{id}` - Delete shift (if not started)
- `POST /api/shifts/{id}/clock-in` - Staff clocks in for shift
- `POST /api/shifts/{id}/clock-out` - Staff clocks out from shift
- `POST /api/shifts/{id}/break-start` - Start break
- `POST /api/shifts/{id}/break-end` - End break

## Notes on API Design
- All endpoints are prefixed with `/api` and versioned (e.g., `/api/v1/...`) - we omitted version for brevity but recommend including.
- Data transfer objects (DTOs) are used to shape requests and responses.
- Error handling returns standard HTTP status codes and error messages.
- Authentication middleware validates JWT tokens for protected endpoints.
- Role-based authorization is enforced at the endpoint or service level.
- Pagination is implemented using `page` and `pageSize` query parameters.
- Filtering, sorting, and searching are supported via query parameters where applicable.
- Dates are transmitted in ISO 8601 format.
- Decimal values for money are represented as strings or numbers with fixed precision (using Decimal128 in MongoDB).

