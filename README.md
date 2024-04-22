## edge-sql

### MS SQL Server compiler for [Edge.js](https://github.com/agracio/edge-js). 

### This library is based on https://github.com/tjanczuk/edge-sql all credit for original work goes to Tomasz Janczuk. 
-------

## Overview
* Supports returning multiple results from `select` queries
* Supports any type of SQL statement allowing to run complex queries that declare variables, temp tables etc...

**NOTE** SQL Server Geography and Geometry types are not supported.

### SQL statement interpretation

| SQL Statement   | C# Implemetation     |
| --------------- | -------------------- |
| select          | ExecuteReaderAsync   |
| update          | ExecuteNonQueryAsync |
| insert          | ExecuteNonQueryAsync |
| delete          | ExecuteNonQueryAsync |
| exec/execute    | ExecuteReaderAsync   |
| other           | ExecuteReaderAsync   |

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
 
##### Using parameterized function

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
 
##### Stored proc with input parameters  

 ```js
const edge = require('edge-js');

var storedProcParams = {inputParm1: 'input1', inputParam2: 25};

var select = edge.func('sql', {
    source: 'exec myStoredProc',
    connectionString: 'SERVER=myserver;DATABASE=mydatabase;Integrated Security=SSPI'
});

select(storedProcParams, function (error, result) {
    if (error) throw error;
    console.log(result);
});
```  
