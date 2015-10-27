# PROG3050 - Veil

### Project Timeline

**Nov 13**
- Team Design

**Dec 18**
- Final Project Presentation

I've created milestones for these deadlines, so put issues related to them under the related milestone.

Our specific week by week plan can be found here:
https://docs.google.com/document/d/1kHlQPMBe7dQ8jpvGUNa2ZDrAGux7Vlwn9OXq5KZhnkE/edit?usp=sharing

### Solution
The two config files needed can be found in our Drive folder. Place these in the Veil project.

#### Setup
In order to register, create credit cards, or checkout completely a Stripe Secret Key must beed added to appSettings with the entry key as ```StripeApiKey```.

In order to use our GearHost SQL Server, you must add the connection string for it to ```ConnectionStrings.config``` under the ```VeilDatabase``` entry. This connection string can be found in our Google Drive folder or by asking Drew for it.  
**DO NOT COMMIT ```ConnectionStrings.config``` with this information in it**
