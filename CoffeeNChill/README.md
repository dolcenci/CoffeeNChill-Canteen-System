 CoffeeNChill Canteen Management System

 Overview
Cloud-based microservices system for a campus canteen. Replaces paper menus, handwritten orders, 
and filing cabinet documents with Azure Functions, Azure Table Storage, Azure File Share, 
and Docker containerization.

 Team
- Student: phomeleloprosper

 Repository Structure



 Part 1: Azure Functions, Tables, Files & Docker Hub

Completed

✅ Azure Table Storage — MenuItems table with MenuService CRUD operations
✅ 5 Menu HTTP Functions:
POST /api/menu — CreateMenuItem
GET /api/menu — GetAllMenuItems
GET /api/menu/category/{category} — GetMenuItemsByCategory
PUT /api/menu/{category}/{id} — UpdateMenuItem
DELETE /api/menu/{category}/{id} — DeleteMenuItem
✅ 3 Document HTTP Functions (code complete):
POST /api/documents/upload — UploadStaffDocument
GET /api/documents — ListStaffDocuments
GET /api/documents/download/{fileName} — DownloadStaffDocument
✅ Azurite emulator running in isolated Docker container
✅ Dockerfile packaging the function app
✅ Docker image v1.0 pushed to Docker Hub: phomeleloprosper/coffeenchill-functions:v1.0
✅ Postman collection with 8 requests (5 passing)

Known Issue

The 3 document endpoints encounter an authentication mismatch
(MAC signature ... not the same as computed signature) when communicating with
Azurite using the modern Azure Storage SDKs. This is a known SDK ↔ Azurite
compatibility issue. The code is correct for real Azure Storage accounts.

Part 2: Queue Triggers & Docker Compose Orchestration

Completed

✅ OrderModels.cs — Order and OrderEntity classes
✅ OrderService.cs — Queue producer + Orders table writer + status updates
✅ 3 Order Functions:
POST /api/orders/queue — PlaceOrder (enqueues order)
GET /api/orders — GetAllOrders
Queue trigger on order-processing-queue — ProcessOrderQueue
✅ Orders table with status lifecycle: Received → Preparing → Ready → Collected
✅ Docker image v2.0 pushed to Docker Hub: phomeleloprosper/coffeenchill-functions:v2.0
✅ docker-compose.yml orchestrates:
azurite service (storage emulator)
functions service (references Docker Hub image phomeleloprosper/coffeenchill-functions:v2.0)

How to Run Locally

Start Azurite
docker run -d --name coffeenchill-azurite \
  -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite \
  azurite --loose --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0