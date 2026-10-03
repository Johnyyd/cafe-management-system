# Cafe Management System - MongoDB Database Design

## Design Principles
- Use embedded documents for data that is tightly coupled and not duplicated across multiple parent documents.
- Use references (by ID) for data that may be shared across multiple parent documents or when the embedded data would become too large.
- Aim for under 10 collections.
- Each document should represent a single unit of data that is typically accessed together.
- **All collections include soft delete (deletedAt) and audit fields (createdBy, updatedBy) for traceability.**
- **Prices stored as Decimal128 for precise financial calculations.**

## Collections

### 1. Shops
Contains information about each coffee shop location.

```json
{
  "_id": ObjectId,
  "name": "string",
  "address": {
    "street": "string",
    "city": "string",
    "district": "string",
    "zipCode": "string"
  },
  "contact": {
    "phone": "string",
    "email": "string"
  },
  "operatingHours": [  // Array of objects for each day of week (0-6, 0=Sunday)
    {
      "dayOfWeek": "number",
      "openTime": "string",  // HH:mm format
      "closeTime": "string"
    }
  ],
  "status": "enum",  // active, under_maintenance, closed
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",  // null if not deleted
  "createdBy": "ObjectId",  // Reference to Staff/Admin who created
  "updatedBy": "ObjectId"   // Reference to Staff/Admin who last updated
}
```

### 2. Staff
Information about employees. References the shop they are currently assigned to.

```json
{
  "_id": ObjectId,
  "firstName": "string",
  "lastName": "string",
  "role": "enum",  // barista, cashier, manager, admin
  "contact": {
    "phone": "string",
    "email": "string"
  },
  "employmentStatus": "enum",  // active, on_leave, terminated
  "hireDate": "ISODate",
  "shopId": ObjectId,  // Reference to Shops
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 3. MenuItems
Items available for sale. Embedded ingredients and allergens arrays. Each shop can have its own menu.

```json
{
  "_id": ObjectId,
  "shopId": ObjectId,  // Reference to Shops (if menus are shop-specific) or null for global menu
  "category": "string",  // e.g., Coffee, Tea, Pastry, Sandwich
  "name": "string",
  "description": "string",
  "price": "Decimal128",  // Store as Decimal128 for precise financial calculations
  "ingredients": [ "string" ],  // List of ingredient names
  "allergens": [ "string" ],   // e.g., nuts, dairy, gluten
  "availability": {
    "startTime": "string",  // HH:mm, optional if available all day
    "endTime": "string",
    "daysOfWeek": [ "number" ]  // 0-6, empty array means every day
  },
  "status": "enum",  // available, unavailable, seasonal
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 4. Inventory
Tracks stock of ingredients and supplies per shop.

```json
{
  "_id": ObjectId,
  "shopId": ObjectId,  // Reference to Shops
  "itemName": "string",
  "unit": "string",  // e.g., kg, g, liters, pieces
  "quantity": "number",
  "reorderLevel": "number",
  "supplierId": ObjectId,  // Reference to Suppliers (can be null if not tracked)
  "lastUpdated": "ISODate",
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 5. Suppliers
Information about suppliers for inventory items.

```json
{
  "_id": ObjectId,
  "name": "string",
  "contact": {
    "person": "string",
    "phone": "string",
    "email": "string"
  },
  "materialsSupplied": [ "string" ],  // e.g., coffee beans, milk, bread
  "rating": "number",  // 1-5
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 6. Orders
Customer orders. Embeds customer information and ordered items (which denormalizes menu item details at the time of order).

```json
{
  "_id": ObjectId,
  "shopId": ObjectId,  // Reference to Shops
  "staffId": ObjectId,  // Reference to Staff (cashier who took the order)
  "customerInfo": {
    "name": "string",  // optional for anonymous customers
    "contact": "string",  // phone or email for loyalty/notifications
    "type": "enum"  // dine_in, takeaway, online
  },
  "items": [  // Array of ordered items
    {
      "menuItemId": ObjectId,  // Reference to MenuItems (for tracking, but we denormalize below)
      "name": "string",  // Denormalized from MenuItems at time of order
      "description": "string",  // optional
      "unitPrice": "Decimal128",  // Price at time of order (Decimal128)
      "quantity": "number",
      "specialInstructions": "string",  // e.g., "extra hot", "no sugar"
      "totalPrice": "Decimal128"  // quantity * unitPrice
    }
  ],
  "status": "enum",  // placed, preparing, ready, completed, cancelled
  "paymentStatus": "enum",  // pending, paid, failed, refunded
  "totalAmount": "Decimal128",
  "orderTime": "ISODate",
  "completedTime": "ISODate",  // null until completed
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 7. QAChecklists
Defines checklists for service or product quality assurance.

```json
{
  "_id": ObjectId,
  "shopId": ObjectId,  // Reference to Shops (can be global if same for all shops)
  "type": "enum",  // service, product
  "name": "string",  // e.g., "Opening Service Checklist", "Latte Product Checklist"
  "criteria": [  // Embedded array of checklist items
    {
      "description": "string",
      "standard": "string",  // Expected standard (e.g., "Espresso shot time: 25-30s")
      "points": "number"  // Points awarded if met (e.g., 2 points)
    }
  ],
  "frequency": "enum",  // daily, per_shift, weekly
  "isActive": "boolean",
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 8. QARecords
Records of performed QA checks, referencing the checklist and including findings.

```json
{
  "_id": ObjectId,
  "checklistId": ObjectId,  // Reference to QAChecklists
  "shopId": ObjectId,  // Reference to Shops
  "staffId": ObjectId,  // Reference to Staff (the inspector)
  "score": "number",  // Total points achieved
  "maxScore": "number",  // Total possible points (can be derived from checklist but stored for snapshot)
  "findings": [  // Embedded array of issues or notes
    {
      "criterionDescription": "string",
      "observed": "string",  // What was observed
      "expected": "string",  // What was expected per standard
      "severity": "enum"  // minor, major, critical
    }
  ],
  "inspectorNotes": "string",
  "date": "ISODate",  // When the check was performed
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 9. CustomerFeedback
Feedback from customers, optionally linked to an order.

```json
{
  "_id": ObjectId,
  "orderId": ObjectId,  // Reference to Orders (can be null for general feedback)
  "shopId": ObjectId,  // Reference to Shops
  "ratingOverall": "number",  // 1-5
  "ratingService": "number",  // 1-5
  "ratingProduct": "number",  // 1-5
  "comments": "string",
  "date": "ISODate",
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

### 10. Shifts
Work shifts for staff, referencing shop and staff.

```json
{
  "_id": ObjectId,
  "shopId": ObjectId,  // Reference to Shops
  "staffId": ObjectId,  // Reference to Staff
  "startTime": "ISODate",
  "endTime": "ISODate",
  "breakDuration": "number",  // in minutes
  "notes": "string",
  "createdAt": "ISODate",
  "updatedAt": "ISODate",
  "deletedAt": "ISODate",
  "createdBy": "ObjectId",
  "updatedBy": "ObjectId"
}
```

## Collection Count
We have 10 collections. If we need to reduce, consider:
- Embedding Staff in Shops (if staff do not change shops frequently and we accept duplication for historical shifts). However, we have Shifts collection that already references both, so we can keep Staff separate and reference.
- Combining Suppliers into Inventory as an embedded supplier document? But then we lose the ability to track supplier performance across multiple inventory items. We'll keep as is.

## Indexing Strategy (brief)
- Shops: _id (primary), status, createdAt
- Staff: _id, shopId, employmentStatus, createdAt
- MenuItems: _id, shopId, status, category, createdAt
- Inventory: _id, shopId, itemName, quantity, createdAt
- Suppliers: _id, rating, createdAt
- Orders: _id, shopId, staffId, status, paymentStatus, orderTime, createdAt
- QAChecklists: _id, shopId, type, isActive, createdAt
- QARecords: _id, checklistId, shopId, staffId, date, createdAt
- CustomerFeedback: _id, orderId, shopId, date, createdAt
- Shifts: _id, shopId, staffId, startTime, endTime, createdAt

**All collections have indexes on deletedAt for soft delete filtering and createdAt for audit queries.**

## Notes on Embedding
- We embedded customer information in Orders to avoid needing a separate Customers collection and to capture details at the time of order.
- We embedded menu item details (name, price) in Orders.items to have a snapshot of what was ordered and at what price, even if the menu changes later.
- We embedded checklist criteria in QAChecklists and findings in QARecords for similar reasons.
- Inventory items reference Suppliers but do not embed supplier details to avoid duplication and allow supplier updates to reflect across inventory.
- **All collections include soft delete (deletedAt) and audit fields (createdBy, updatedBy) for full traceability.**
- **Prices use Decimal128 for precise financial calculations without floating-point errors.**

