CoffeeNChill Canteen Management System

CoffeeNChill is a cloud based canteen management system developed to improve how food orders, menu information, and staff documents are managed on a campus. The system replaces manual processes, such as paper menus, handwritten orders, and physical document storage, with digital services built using Azure Functions, Azure Storage, and Docker.

Project Status

Part 1 is complete and includes the menu API, document API, and Azure Storage integration.

Part 2 is complete and includes order processing through queues, queue triggered functions, and Docker Compose orchestration.

Part 3 is currently in progress and covers the API Gateway, JWT authentication, and VPS deployment. This part is being submitted separately.

Repository Structure

The main project directory is CoffeeNChill Canteen System.

Inside the CoffeeNChill directory, the CoffeeNChill.Functions project contains the Azure Functions application.

The Functions folder contains MenuFunctions.cs, which handles menu related HTTP requests, DocumentFunctions.cs, which manages document operations, and OrderFunctions.cs, which handles order submission and queue triggered processing.

The Services folder contains MenuService.cs for menu data operations, DocumentService.cs for file share operations, OrderService.cs for order queue and table operations, and OrderModels.cs for the order data models.

Program.cs contains the dependency injection and application host configuration. The Dockerfile defines the application container. The host.json file contains Azure Functions host settings, while local.settings.json contains local development configuration.

The root directory also contains CoffeeNChill.slnx, docker compose configuration, Postman collection and environment files, the gitignore file, and this README document.

Part 1. Azure Functions, Storage, and Docker Hub

The first part of the project focuses on developing the menu and document services and connecting them to Azure Storage.

The menu service provides five HTTP endpoints for managing canteen menu items. The create operation adds a new item to the MenuItems Azure Table. The retrieval operation returns all available menu items, while another endpoint allows users to retrieve items belonging to a particular category. The update operation modifies an existing item, and the delete operation removes an item from the table.

The menu endpoints are listed below.

CreateMenuItem uses POST at /api/menu.

GetAllMenuItems uses GET at /api/menu.

GetMenuItemsByCategory uses GET at /api/menu/category/{category}.

UpdateMenuItem uses PUT at /api/menu/{category}/{id}.

DeleteMenuItem uses DELETE at /api/menu/{category}/{id}.

The document service provides three endpoints for working with staff documents stored in the staff-docs Azure File Share. Users can upload documents through a multipart form request, retrieve a list of stored files, and download a selected document using its filename.

The document endpoints are listed below.

UploadStaffDocument uses POST at /api/documents/upload.

ListStaffDocuments uses GET at /api/documents.

DownloadStaffDocument uses GET at /api/documents/download/{fileName}.

The Docker image created for the first part of the project is phomeleloprosper/coffeenchill-functions:v1.0.

Known Issue With File Share Authentication

The document endpoints have been implemented, but requests to Azurite encounter a signature mismatch when using the current Azure Storage SDK configuration. This affects communication with the local storage emulator. The implementation is intended to work with a properly configured Azure Storage account, but successful operation against a real Azure account must be verified separately.

Part 2. Queue Processing and Docker Compose

The second part introduces asynchronous order processing to improve the handling of incoming canteen orders.

The order submission endpoint accepts order information in JSON format and places a message on the order-processing-queue. This operation is handled by the PlaceOrder function through POST at /api/orders/queue.

When a new message enters the queue, the ProcessOrderQueue function is triggered automatically. The function converts the message into an Order object and records the order in the Orders Azure Table. The OrderDate is used as the PartitionKey, while the OrderId is used as the RowKey. Newly recorded orders receive an initial status of Received.

The processing workflow simulates the stages of a typical canteen order. An order begins with the Received status, moves to Preparing, changes to Ready, and eventually reaches Collected.

The system also provides an endpoint for retrieving stored orders. The GetAllOrders function uses GET at /api/orders to return the available order records and their current statuses.

The Docker image for the second part is phomeleloprosper/coffeenchill-functions:v2.0.

Purpose of Asynchronous Processing

During busy periods, such as breaks between lectures, many students may submit orders at approximately the same time. Processing every order immediately within the original HTTP request can place unnecessary pressure on the application.

Queue based processing separates order submission from order preparation. The application can place an order message in the queue and respond to the client while the background function handles the remaining processing. This approach helps manage bursts of incoming requests and reduces the need to perform the entire workflow within a single request.

Running the Project Locally

Prerequisites

Docker Desktop must be installed and running.

Visual Studio 2022 and the .NET 8 SDK are required for development.

Postman is recommended for testing the HTTP endpoints.

Step 1. Start Azurite

Run the following command in Command Prompt to start the local Azure Storage emulator.

docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite azurite --loose --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0

This command starts Azurite and exposes the ports used by its Blob, Queue, and Table services.

Step 2. Run the Azure Functions Application

Open CoffeeNChill/CoffeeNChill.slnx in Visual Studio.

Press F5 to start the application.

The Functions host is expected to be available at http://localhost:7051 when using the configured local development environment.

Step 3. Test the Endpoints Using Postman

Import postman-collection.json and postman-environment.json into Postman.

Select the CoffeeNChill Local environment.

Confirm that the baseUrl variable is set to http://localhost:7051.

Confirm that the authCode variable is set to test. This value is intended for the local testing configuration and must match the function key accepted by the application. The local.settings.json configuration uses file based storage for Azure Functions host secrets.

Execute the requests in the collection to test the application's functionality.

Running the Project With Docker Compose

The repository includes a docker-compose.yml file that coordinates the storage emulator and the Azure Functions container.

Open a terminal in the CoffeeNChill Canteen System root directory and execute the following command.

docker compose up -d

This starts the coffeenchill-azurite storage emulator and the coffeenchill-functions application using the published image phomeleloprosper/coffeenchill-functions:v2.0.

To stop the containers, run the following command.

docker compose down

API Endpoint Reference

Menu Operations

CreateMenuItem uses POST at /api/menu and requires a function key.

GetAllMenuItems uses GET at /api/menu and requires a function key.

GetMenuItemsByCategory uses GET at /api/menu/category/{category} and requires a function key.

UpdateMenuItem uses PUT at /api/menu/{category}/{id} and requires a function key.

DeleteMenuItem uses DELETE at /api/menu/{category}/{id} and requires a function key.

Document Operations

UploadStaffDocument uses POST at /api/documents/upload and requires a function key.

ListStaffDocuments uses GET at /api/documents and requires a function key.

DownloadStaffDocument uses GET at /api/documents/download/{fileName} and requires a function key.

Order Operations

PlaceOrder uses POST at /api/orders/queue and requires a function key.

GetAllOrders uses GET at /api/orders and requires a function key.

ProcessOrderQueue is triggered by messages on the order-processing-queue and does not use an HTTP authentication key because it is a queue triggered function.

Docker Hub Images

The Part 1 image is phomeleloprosper/coffeenchill-functions with the v1.0 tag.

The Part 2 image is phomeleloprosper/coffeenchill-functions with the v2.0 tag.

To retrieve the latest image for Part 2, run the following command.

docker pull phomeleloprosper/coffeenchill-functions:v2.0

Sample Postman Requests

Submitting an Order

Send a POST request to the following address.

http://localhost:7051/api/orders/queue?code=test

Use the following JSON as the request body.

{
  "OrderId": "ORD-2026-3001",
  "CustomerName": "Thabo Nkosi",
  "SelectedItemSKUs": ["HD-2026-001", "PS-2026-001"],
  "TotalPrice": 7.20,
  "OrderTimestamp": "2026-10-09T10:15:00Z"
}

The expected successful response is shown below.

{
  "Message": "Order placed successfully",
  "OrderId": "ORD-2026-3001",
  "Status": "Received (Processing)"
}

The exact response depends on the implementation of the PlaceOrder function.

Retrieving All Orders

Send a GET request to the following address.

http://localhost:7051/api/orders?code=test

The endpoint returns an array containing the stored orders and their current processing statuses.

Technology Stack

The application uses .NET 8 and the isolated worker model for Azure Functions.

Azure Functions version 4 provides the serverless execution environment for the application's functions.

Azure Table Storage stores menu items and order information.

Azure Queue Storage manages background order processing through the order-processing-queue.

Azure File Shares provide storage for staff documents.

Azurite provides a local environment for testing Azure Storage functionality.

Docker packages the application into a container, while Docker Compose coordinates the services required to run the application locally.

Postman is used to send requests and verify the API responses.

Author

Dolcenci

This project was developed as an individual university assignment.

License and Usage

This repository was created for educational purposes as part of a university assignment.

Next Development Steps

The next stage is to execute the four main Postman tests covering order submission, order retrieval, menu creation, and menu retrieval.

The requests to test are POST /api/orders/queue, GET /api/orders, POST /api/menu, and GET /api/menu.

Record the HTTP response status and response body returned by each request. In particular, confirm whether the order submission endpoint returns HTTP 200 OK or another successful status code.

Once the tests have been completed and the results reviewed, the project can be committed and pushed to GitHub with the API Gateway excluded from the submission where required.
