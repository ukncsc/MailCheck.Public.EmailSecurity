# MailCheck.EmailSecurity
The Mail Check EmailSecurity Microservice is responsible for evaluating multiple microservice data and handling changes for an entity.
You should have a database connection established with the User DB when running any project which contains a DAO folder.
## Environment Variables for running:
In general, when running any project within MailCheck.EmailSecurity you should have the following environment variables set up:
|Variable  | Value |
|--|--|
| SnsTopicArn | arn of topic to publish messages  |
| MicroserviceOutputSnsTopicArn |arn of topic for api to publish messages
| ConnectionString |database connection string | 
| DevMode | boolean to toggle CORS and run on localhost | 
| NameServer | NameServer used in the Poller
| AWS_REGION | aws datacentre region  |
| AWS_ACCESS_KEY_ID |aws access key  |
| AWS_SECRET_ACCESS_KEY |aws secret access key  |
