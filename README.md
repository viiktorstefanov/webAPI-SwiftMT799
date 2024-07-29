# webAPI SWIFT MT799 Messages

## Overview

This application provides a RESTful API for handling SWIFT MT799 messages. It allows you to upload SWIFT MT799 messages and get stored messages. The application uses ASP.NET Core, NLog for logging, and SQLite for storage.<br><br>
A Swift MT799 message is a type of financial message used in banking to communicate between institutions and banks. It's primarily used for sending text messages regarding transactions or banking arrangements. Often used for informal communication related to trade or financial agreements.<br>
In SWIFT MT799 messages, the different blocks of the message are defined by specific opening and closing delimiters. The specific formatting of these delimiters helps ensure that the messages are correctly parsed and interpreted by the receiving systems. <br>
<br>Structure of SWIFT MT799 Message:<br>
1.	{1: Basic Header Block: This block contains information about the sender, including the Bank Identifier Code (BIC), session number, and sequence number}
2.	{2: Application Header Block: This block includes details about the message type and the receiver}
3.	{4: Text Block: This block contains the main body of the message, which is free-format text. Because the length and content of this block can vary greatly, a specific closing delimiter (-}) is used to clearly indicate the end of this block}
4.	{5: Trailer Block: This block contains a Message Authentication Code (MAC) for integrity verification and a Check (CHK) field provides a checksum or hash value to help ensure the integrity of the message}


## Features

- **Upload SWIFT MT799 Messages**: Upload messages in SWIFT MT799 format and store them in a SQLite database.
- **Get All Messages**: Fetch all stored SWIFT MT799 messages.
- **Logging**:  Logging using NLog.
- **API Documentation**: Swagger UI for easy exploration and testing of the API.

## Requirements

- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQLite](https://www.sqlite.org/download.html)

## Installation

1. **Clone the Repository**

    ```sh
    git clone https://github.com/viiktorstefanov/webAPI-SwiftMT799.git
    cd webAPI-SwiftMT799
    ```

2. **Install Dependencies**

    Ensure you have the .NET SDK installed, then run:

    ```sh
    dotnet restore
    ```

3. **Setup Database**

    The application uses SQLite. The database is initialized automatically when the application starts.

## Running the Application

1. **Build and Run**

    ```sh
    dotnet build
    dotnet run
    ```

2. **Access the API**

    - **Swagger UI**: Navigate to `https://localhost:5000/swagger` to explore and test the API endpoints.
    - **API Endpoints**:
        - **GET /api/message/messages**: Get all messages.
        - **POST /api/message/upload**: Upload a SWIFT MT799 message file.

## API Endpoints

### **GET /api/message/messages**

- **Description**: Get all stored messages.
- **Responses**:
  - **200 OK**: List of messages.
  - **404 Not Found**: No messages found.
  - **500 Internal Server Error**: Unexpected error.

### **POST /api/message/upload**

- **Description**: Upload a file containing a SWIFT MT799 message.
- **Request**: Requires a file upload with SWIFT MT799 message content.
- **Responses**:
  - **200 OK**: Message successfully saved.
  - **400 Bad Request**: Invalid file format or content.
  - **500 Internal Server Error**: Unexpected error.

## Additional Information

### Files in the Repository

- **`Task.txt`**: Contains the requirements and specifications for the application. Read this file to understand expectations for the project.
- **`example_mt799.txt`**: An example SWIFT MT799 message file that you can use for testing the API. This file contains sample content formatted according to the SWIFT MT799 specification.

