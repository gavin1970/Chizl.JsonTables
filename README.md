# ![Logo1](https://github.com/gavin1970/Chizl.JsonTables/blob/master/Chizl.JsonTables/imgs/Chizl.JsonTables_200.png)
# Chizl.JsonTables

[![NuGet version (Chizl.JsonTables)](https://img.shields.io/nuget/v/Chizl.JsonTables.svg?style=flat-square)](https://www.nuget.org/packages/Chizl.JsonTables/)

Chizl.JsonTables uses the popular Newtonsoft.Json Library with an extention of DataSets and DataTables.  You use this library as you would DataSets and it produces JSON including schemas of your dataset for you.  All data is in memory as a DataSet to make it fast and auto updates the Json for all data changes.  Structure changes require a Flush().
This libary also supports SecuredString Columns.  This means, you can have a PI data in memory that is secured and AES encrypted that is converted to Base64 strings in your Json.  This is done via AES Encryption with salted passwords you pass into constructor.  

#### Not Recommended <img src="fa-crown.svg" width="50" height="50">
Salted password is not required, however default Key/Vector will be used for encrypting any SecureString columns in JSON.

## Build with
- Microsoft Visual Studio Professional 2022 (64-bit)

## Supported by
- netstandard2.0
- netstandard2.1
- net47
- net48
- net6.0
- net7.0

## Dependency
- Newtonsoft.Json v13.0.3

## Example of use
- [net6.0 Demo Included](https://github.com/gavin1970/Chizl.JsonTables/tree/master/DemoConsole)

## Links
- [Homepage](http://www.chizl.com/Chizl.JsonTables)
- [Documentation](http://www.chizl.com/Chizl.JsonTables/help)
- [NuGet Package](https://www.nuget.org/packages/Chizl.JsonTables)
- [Release Notes](https://github.com/gavin1970/Chizl.JsonTables/releases)
- [Contributing Guidelines](https://github.com/gavin1970/Chizl.JsonTables/blob/master/CONTRIBUTING.md)
- [License](https://github.com/gavin1970/Chizl.JsonTables/blob/master/Chizl.JsonTables/docs/LICENSE.md)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/Chizl.JsonTables)
