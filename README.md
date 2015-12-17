# PROG3050 - Veil

### Project Timeline

**Done**

There are milestones and labels for each of the phases / deadlines of the project.

### Solution
The two config files needed can be found in our Drive folder. Place these in the Veil project.

#### Setup
In order to register, create credit cards, or checkout completely, a Stripe Secret Key must be added to *PrivateAppSettings.config* with the entry key as ```StripeApiKey```.

In order to register and login, a SendGrid API Key must be added to *PrivateAppSettings.config* with the entry key as ```SendGridApiKey``` and a sender email must be added as ```SenderEmailAddress```.

In order to use our GearHost SQL Server, you must add the connection string for it to ```ConnectionStrings.config``` under the ```VeilDatabase``` entry. This connection string can be found in our Google Drive folder or by asking Drew for it.  
**DO NOT COMMIT ```ConnectionStrings.config``` with this information in it**
