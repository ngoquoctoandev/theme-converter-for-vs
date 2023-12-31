// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

/* 
 Setup: Enter your storage account name and shared key in main()
*/

const { BlobServiceClient } = require("@azure/storage-blob");

// Load the .env file if it exists
require("dotenv").config();

async function main() {
    // Create Blob Service Client from Account connection string or SAS connection string
    // Account connection string example - `DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=accountKey;EndpointSuffix=core.windows.net`
    // SAS connection string example - `BlobEndpoint=https://myaccount.blob.core.windows.net/;QueueEndpoint=https://myaccount.queue.core.windows.net/;FileEndpoint=https://myaccount.file.core.windows.net/;TableEndpoint=https://myaccount.table.core.windows.net/;SharedAccessSignature=sasString`
    const STORAGE_CONNECTION_STRING = process.env.STORAGE_CONNECTION_STRING || "";
    // Note - Account connection string can only be used in node.
    const blobServiceClient = BlobServiceClient.fromConnectionString(STORAGE_CONNECTION_STRING);

    let index = 1;
    for await (const container of blobServiceClient.listContainers()) {
        console.log(`Container ${index++}: ${container.name}`);
    }

    // Create a container
    const containerName = `newcontainer${new Date().getTime()}`;
    const containerClient = blobServiceClient.getContainerClient(containerName);

    const createContainerResponse = await containerClient.create();
    console.log(`Create container ${containerName} successfully`, createContainerResponse.requestId);

    // Delete container
    await containerClient.delete();

    console.log("deleted container");
}

main().catch((err) => {
    console.error("Error running sample:", err.message);
});