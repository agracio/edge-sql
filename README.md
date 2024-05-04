## edge-sql

### MS SQL Server compiler for [Edge.js](https://github.com/agracio/edge-js). 

### This library is based on https://github.com/tjanczuk/edge-sql all credit for original work goes to Tomasz Janczuk. 
-------

## Overview
* Supports returning multiple results from `select` queries
* Supports any type of SQL statement allowing to run complex queries that declare variables, temp tables etc...
* Supports stored procedures with return parameters
* Supports .NET Framework 4.5 and .NET Standard 2.0

**NOTE** SQL Server Geography and Geometry types are not supported.

### SQL statement interpretation

| SQL Statement   | C# Implemetation     |
| --------------- | -------------------- |
| select          | ExecuteReaderAsync   |
| update          | ExecuteNonQueryAsync |
| insert          | ExecuteNonQueryAsync |
| delete          | ExecuteNonQueryAsync |
| exec/execute    | ExecuteReaderAsync*  |
| other           | ExecuteReaderAsync   |

***Stored procedures with output parameters are executed using ExecuteNonQueryAsync, see examples below**

### Parameters

| Parameter          | Usage                |
| ------------------ | -------------------- |
| `connectionString` | Required. Use environment variable or input parameter |
| `source`           | Optional if no other parameters are specified         |
| `commandTimeout`   | Optional                                              |

To better understand parameter usage see examples below.

### Basic usage

You can set your SQL connection string using environment variable. For passing connection string as a parameter see [Advanced usage](#advanced-usage).

```
set EDGE_SQL_CONNECTION_STRING=Data Source=localhost;Initial Catalog=Northwind;Integrated Security=True
```

#### Simple select

```js
const edge = require('edge-js');

var getTop10Products = edge.func('sql', function () {/*
    select top 10 * from Products
*/});

getTop10Products(null, function (error, result) {
    if (error) throw error;
    console.log(result);
});
```

#### Parameterized queries

You can construct a parameterized query once and provide parameter values on a per-call basis:

**SELECT**

```js
const edge = require('edge-js');

var getProduct = edge.func('sql', function () {/*
    select * from Products 
    where ProductId = @myProductId
*/});

getProduct({ myProductId: 10 }, function (error, result) {
    if (error) throw error;
    console.log(result);
});
```

**UPDATE**

```js
const edge = require('edge-js');

var updateProductName = edge.func('sql', function () {/*
    update Products
    set ProductName = @newName 
    where ProductId = @myProductId
*/});

updateProductName({ myProductId: 10, newName: 'New Product' }, function (error, result) {
    if (error) throw error;
    console.log(result);
});
```

### Advanced usage
 
#### Using parameterized function

```js
const edge = require('edge-js');

var select = edge.func('sql', {
    source: 'select top 10 * from Products',
    connectionString: 'SERVER=myserver;DATABASE=mydatabase;Integrated Security=SSPI',
    commandTimeout: 100
});

select(null, function (error, result) {
    if (error) throw error;
    console.log(result);
});
```

#### Select with multiple results

```js
const edge = require('edge-js');

var select = edge.func('sql', {
    source: 'select top 5 * from Authors; select top 5 * from Books',
    connectionString: 'SERVER=myserver;DATABASE=mydatabase;Integrated Security=SSPI',
    commandTimeout: 100
});

select(null, function (error, result) {
    if (error) throw error;
    console.log(result);
});

```

Result

```js
{
  Authors: [
    { Id: 1, Name: 'Author - 1', Country: 'Country - 1' },
    { Id: 2, Name: 'Author - 2', Country: 'Country - 2' },
    { Id: 3, Name: 'Author - 3', Country: 'Country - 3' },
    { Id: 4, Name: 'Author - 4', Country: 'Country - 4' },
    { Id: 5, Name: 'Author - 5', Country: 'Country - 5' }
  ],
  Books: [
    { Id: 1, Author_id: 485, Price: 64, Edition: 9 },
    { Id: 2, Author_id: 310, Price: 53, Edition: 8 },
    { Id: 3, Author_id: 138, Price: 86, Edition: 3 },
    { Id: 4, Author_id: 88, Price: 62, Edition: 5 },
    { Id: 5, Author_id: 165, Price: 91, Edition: 2 } 
  ]
}
```
 
#### Stored proc with input parameters  

 ```js
const edge = require('edge-js');

var params = {inputParm1: 'input1', inputParam2: 25};

var execProc = edge.func('sql', {
    source: 'exec myStoredProc',
    connectionString: 'SERVER=myserver;DATABASE=mydatabase;Integrated Security=SSPI'
});

execProc(params, function (error, result) {
    if (error) throw error;
    console.log(result);
});
```  
#### Stored proc with output parameters

Example SQL 

```sql
CREATE Table Authors
(
   Id int identity primary key,
   Name nvarchar(50),
   Country nvarchar(50)
)

CREATE PROCEDURE GetAuthorDetails
(
    @AuthorID INT,
    @Name NVARCHAR(100) OUTPUT,
    @Country NVARCHAR(100) OUTPUT
)
AS
BEGIN
    SELECT @Name = Name, @Country = Country
    FROM Authors WHERE Id = @AuthorID
END
```

Javascript

* Return parameter **names** have to start with **@** 
* Return parameter **values** have to correspond to stored proc output names
* Return values are converted to string
* Result will return stored proc output names <br/> <br/>
  
  
```js
const edge = require('edge-js');

var execProc = edge.func('sql', {
    source: 'exec GetAuthorDetails',
    connectionString: 'SERVER=myserver;DATABASE=mydatabase;Integrated Security=SSPI'
});

execProc({ AuthorID: 1, '@return1': 'Name', '@return2': 'Country' }, function (error, result) {
    if (error) throw error;
    console.log(result);
});
```  

Result

```js
{ Name: 'Author - 1', Country: 'Country - 1' }
```
