# RapidApi
A friendly way to bootstrap an odata service for when you want to test/evaluate the OData protocol.

The cli creates a fully functional project that you can deploy on azure to test your service and hence allows you to be able to get started much faster.

## How to run.

Clone the project and run the following command.

```cmd
dotnet run --project RapidApi\RapidApi.csproj --schema "C:\Users\UserName\source\repos\schema.xml" --app "MyOdataService"
```

Upon successful deployment the application gives out a url for the service id.

## Consuming the service on .Net or your Xamarin app.

1. On visual studio install the[ OData Connected service extension](https://marketplace.visualstudio.com/items?itemName=laylaliu.ODataConnectedService).
2. [Generate the client](https://devblogs.microsoft.com/odata/odata-connected-service-0-4-0-release/) code using the OData Connected service. 
3. Enjoy. 

For other languages and platforms kindly consult [Odata.org/Libraries](https://www.odata.org/libraries/)
