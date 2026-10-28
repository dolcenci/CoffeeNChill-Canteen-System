# CoffeeNChill Canteen Management System

This repository contains the complete source code for the CoffeeNChill Canteen Management System. The project was built incrementally across three parts to modernize the campus canteen operations. It replaces traditional paper menus, handwritten order slips, and filing cabinet documents with a modern, cloud-enabled microservices architecture using Azure Functions, Azure Table Storage, Azure File Shares, and Docker containerization.

Below is a detailed breakdown of the work completed for each part of the assignment.

## Part 1: Azure Functions, Tables, Files and Docker Hub

In the first part of the project, the goal was to establish the cloud foundation by replacing paper menus and physical documents with digital storage solutions.

I created an Azure Functions project in Visual Studio using the .NET 8 isolated worker model. The project includes a Menu API and a Document API. The Menu API consists of five HTTP-triggered endpoints that perform Create, Read, Update, and Delete operations. These operations are connected to an Azure Table named MenuItems, where the PartitionKey is used for the category (such as Hot Drinks) and the RowKey is used for the unique item SKU. The Document API consists of three HTTP-triggered endpoints allowing staff to upload, list, and download operational documents. These files are stored in an Azure File Share named staff-docs.

For local development, Azurite was used as the storage emulator, running inside a standalone Docker container. I also wrote a Dockerfile to package the Azure Functions app and successfully built and pushed the version 1.0 image to my public Docker Hub repository.

During the testing of the Document API, I encountered a specific technical limitation. The modern Azure Storage SDKs have a known authentication mismatch when communicating with the Azurite file share emulator. While the code for the upload, list, and download endpoints is fully complete and would work correctly against a real Azure Storage account, the local Azurite emulator rejects the authentication signatures. This limitation is documented in the code comments.

## Part 2: Queue Triggers and Docker Compose Orchestration

The second part focused on solving the issue of synchronous order processing during peak lecture breaks, which often caused the system to lag.

To fix this, I introduced an asynchronous order workflow using Azure Queue Storage. I created an HTTP endpoint that acts as a queue producer. When a student places an order, the details are serialized into a JSON string and pushed to a queue named order-processing-queue. 

I then created a queue-triggered function named ProcessOrderQueue. This background function automatically fires as soon as a message arrives in the queue. It parses the JSON payload, writes a new entry into an Azure Table named Orders, and then simulates the barista lifecycle. It updates the order status through the stages of Received, Preparing, Ready, and Collected. This ensures the client receives an immediate response without waiting for the actual food preparation to finish.

To run this locally, I updated the Dockerfile and pushed a version 2.0 image to Docker Hub. I also created a docker-compose.yml file at the root of the repository. This file orchestrates both the Azurite container and the Azure Functions container, pulling the version 2.0 image directly from Docker Hub, allowing the entire system to start with a single command.

## Part 3: Web API Gateway, JWT Authentication and VPS Deployment

The final part involved building a secure API gateway to unify access to the services for mobile apps, student portals, and staff tills.

I built a new ASP.NET Core Web API project named CoffeeNChill.Gateway. This gateway serves as a facade, routing requests from clients to the backend Azure Functions. I integrated Swashbuckle to provide a full Swagger UI, enabling XML documentation for all endpoints, request bodies, and response codes.

To secure the platform, I implemented JWT authentication. User profiles are stored in an Azure Table named Users, where the PartitionKey represents the role and the RowKey is the user's email. Passwords are securely hashed using BCrypt before being saved. The authentication endpoints allow users to register and log in. Upon a successful login, the system generates a JWT Bearer token containing the user's name, email, and role.

I applied Role-Based Access Control across the gateway. Public access is allowed for students to view the menu and place orders without logging in. However, endpoints that modify menu prices, upload operational files, or update order statuses are strictly protected. They require a valid JWT token and are restricted to users with the Admin or Staff roles.

Deployment Challenges and Live URL:
The assignment required deploying the complete stack to a live Linux VPS. I initially attempted to provision a free VPS using Oracle Cloud and later a platform called RAW. Unfortunately, both providers strictly require a valid credit card for identity verification, which I was unable to provide at the time.

As a workaround, I deployed the API Gateway as a Docker container to Render, a free Platform-as-a-Service provider. 
The live Swagger UI can be accessed here: https://coffeenchill-gatewayrendernovps.onrender.com/swagger
Please note that because Render is a free tier service, the server spins down after 15 minutes of inactivity. The first request might take up to 50 seconds to wake the server. Additionally, because the backend Azure Functions app is not hosted on Render, API calls executed directly from this live URL will fail to fetch data. The live URL primarily serves to demonstrate the hosted Gateway and Swagger documentation.

CI/CD Pipeline:
As an additional feature, I created a GitHub Actions workflow located in the .github/workflows folder. This pipeline automatically triggers whenever code is pushed to the main branch. It checks out the code, sets up the .NET SDK, builds both the Gateway and Functions Docker images for multiple platforms (linux/amd64 and linux/arm64), and pushes them to Docker Hub.

## How to Run the Project Locally

If you wish to run the full stack on a local machine, follow these steps:

1. Start Azurite by running this command in your terminal:
docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite azurite --loose --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0

2. Open the solution in Visual Studio. Set the CoffeeNChill.Functions project as the startup project and press F5. The functions will run on http://localhost:7051.

3. In a second instance of Visual Studio, set the CoffeeNChill.Gateway project as the startup project and press F5. The gateway will run on http://localhost:5082.

4. Open Postman and import the postman-collection.json and postman-environment.json files provided in the repository. Select the CoffeeNChill Local environment and begin testing the endpoints.

## Docker Hub Images
- phomeleloprosper/coffeenchill-functions:v2.0
- phomeleloprosper/coffeenchill-gateway:v1.0