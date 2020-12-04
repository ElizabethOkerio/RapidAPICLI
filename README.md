# RapidApi

RapidApi allows you to create and deploy an OData mock service based on a provided schema without writing code.

## Prerequisites:
- You need to have [Docker](https://www.docker.com/) installed on your machine in order to run local mock servers
- You need an active [Azure](https://azure.microsoft.com/) subscription in order to deploy remotely. Some commands
will require you to provide an Azure tenant ID and subscription ID

### Finding your Azure tenant ID

Log in to your Azure account portal.
- On the home page of your portal, click Azure Active Directory
- In the Overview section, find the **Tenant Information** card
- Find the **Tenant ID** field and copy its value.
- You can use the `rapidapi config --tenant tenant-id` command to save this tenant ID for later use
with RapidAPI

### Finding your Azure subscription ID
Log in to your Azure account portal
- On the home page of the portal, click **Subscriptions**
- Select the subscription you want to use
- In the Overview section, find the **Subscription ID** field and copy its value.
- You can use the `rapidapi config --subscription subscription-id` command to save this
Subscription ID for later use with RapidApi

## How to install

```
dotnet tool install -g RapidApi.Cli --version 1.0.0-alpha2 --add-source https://identitydivision.pkgs.visualstudio.com/OData/_packaging/ODataTools/nuget/v3/index.json
```

Note, this tool is only available to internal users.

## Authentication

The application needs to access a docker image that's only accessible to internal users (MS Employees),
therefore it will prompt you to sign into your MS account the first time you use the tool.

The application also needs access to your Azure account in order to deploy resources when to the
Azure subscription that you specify. Therefore it will also prompt you to sign into account
associated with the tenant and subscription you provided. This only happens when you're managing
a remote service (deploying, updating or deleting).

So when the app launches a browser window for you to log in, please check the message on the terminal
window to know which account you're expected to log into.

## How to run.

### Running a mock service locally:

```
rapidapi run --schema path/to/schema.xml --port 8090
```

This will generate a mock service with an OData model based on the specified schema file.
The server will run on port 8090. If you don't specify a port, a random one will be chosen.
RapidApi will watch for changes to the schema file and re-start the server when changes are detected.

The API will be generated with an empty data store. If you want to populate it with dummy data, add the `-seed` option:

```
rapidapi run --schema path/to/schema.xml --port 8090 --seed
```

### Deploying remotely

To deploy remotely, use the `deploy` command. You will need to provide an Azure tenant and subscription.

```
rapidapi deploy --schema path/to/schema.xml -app remoteappname --tenant tenant-id --subscription subscription-id
```

This will create, provision and deploy the necessary app resources on Azure under the specified tenant and subscription.
You may be required to authenticate the first time you use that subscription. If a subscription is not provided,
the default subscription for your account will be used.


Upon successful deployment the application displays the URL to use to access the service.

If you have configured a global tenant and/or subscription for use with RapidApi using the `rapidapi config` command, you
can omit those options here:

```
rapidapi deploy --schema path/to/schema.xml --app remoteappname
```

### Updating remote app

To update a previously deployed remote app, run the `update` command providing the new schema.

```
rapidapi update --app remoteappname --schema path/to/updated/schema.xml
```

You can omit the schema path, in which case the tool will try to use
the last schema that was used to deploy your service:

```
rapidapi update --app remoteappname
```

### Deleting a remote app

To delete a previously deployed remote app, run the `delete` command

```
rapidapi delete --app remoteappname
```

### Listing remote apps

You can get a list of remote services deployed using RapidApi by running the `list` command:

```
rapidapi list
```

### Configuring global settings

You can configure default tenant and subscription to be used when deploying services so that
you don't have to pass these options each time.

```
rapidapi config --tenant tenant-id --subscription --subscription-id
```

You can also list your current settings with:

```
rapidapi config
```

### Additional help

To get list of available commands, run:
```
rapidapi --help
```

To get help on a specific command, run:

```
rapidapi [command] --help
```

Example:
```
rapidapi run --help
```

