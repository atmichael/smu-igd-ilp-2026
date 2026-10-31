To configure Gmail to automatically push notifications to a Google Cloud Pub/Sub topic when a new email arrives, you must complete a multi-step setup across the Google Cloud Console and the Gmail API.
You must provision a Pub/Sub topic, grant Gmail explicit publishing rights, create a subscriber endpoint, and call the Gmail API watch method.

Step 1: Set Up Your Google Cloud Project

1. Open the Google Cloud Console.
2. Select or create a Google Cloud project.
3. Navigate to APIs & Services > Library.
4. Search for and Enable both the Gmail API and the Cloud Pub/Sub API. [1] (https://www.unipile.com/gmail-api-push-notifications/), [2] (https://helpcenter.smartlead.ai/en/articles/459-how-to-set-up-google-cloud-pubsub-for-real-time-gmail-sync-private-infrastructure)

Step 2: Create a Pub/Sub Topic and Subscription

1. Go to the Google Cloud Pub/Sub Console. [1] (https://docs.cloud.google.com/pubsub/docs/publish-receive-messages-console)
2. Click Create Topic. [1] (https://docs.cloud.google.com/pubsub/docs/publish-receive-messages-console)
3. Provide a Topic ID (e.g., gmail-notifications) and click Create. Note your full topic name, which looks like:
projects/YOUR_PROJECT_ID/topics/gmail-notifications [1] (https://docs.cloud.google.com/pubsub/docs/publish-receive-messages-console), [2] (https://www.unipile.com/gmail-api-push-notifications/)
4. Go to Subscriptions and click Create Subscription. [1] (https://docs.cloud.google.com/pubsub/docs/publish-receive-messages-console)
5. Select the topic you just created, set the Delivery Type to Push (to send webhooks to your app) or Pull (if your app will query Pub/Sub directly), and click Create. [1] (https://developers.google.com/workspace/gmail/api/guides/push), [2] (https://www.youtube.com/watch?v=pXh2UWLDQ74&t=1)

Step 3: Grant Gmail Publisher Permissions (Crucial Step)

Gmail uses a standard, Google-managed service account to publish events. If you don't grant this account permissions, your setup will silently fail. [1] (https://www.unipile.com/gmail-api-push-notifications/)
1. On your Pub/Sub Topics page, select your new topic.
2. Open the Permissions panel (or click Show Info Panel on the right side).
3. Click Add Principal.
4. In the New principals field, paste exactly:
gmail-api-push@system.gserviceaccount.com
5. Select the role Pub/Sub Publisher and click Save. [1] (https://developers.google.com/workspace/gmail/api/guides/push), [2] (https://www.unipile.com/gmail-api-push-notifications/)

Step 4: Configure OAuth 2.0 Credentials

To tell Gmail to monitor a specific inbox, your application needs authorization from that user.
1. Go to APIs & Services > Credentials.
2. Create an OAuth 2.0 Client ID.
3. Ensure your application requests the minimum required scope:
https://www.googleapis.com/auth/gmail.readonly [1] (https://www.unipile.com/gmail-api-push-notifications/)

Step 5: Start the Gmail "Watch" Session

You must make an authenticated HTTP POST request to the Gmail API users.watch endpoint to initiate notifications. [1] (https://medium.com/@eagnir/understanding-gmails-push-notifications-via-google-cloud-pub-sub-3a002f9350ef)
Endpoint:
http
POST https://googleapis.com
Authorization: Bearer <USER_OAUTH_ACCESS_TOKEN>
Content-Type: application/json
Use code with caution.
Request Body:
json
{
  "topicName": "projects/YOUR_PROJECT_ID/topics/gmail-notifications",
  "labelIds": ["INBOX"],
  "labelFilterAction": "include"
}
Use code with caution.
Setting labelIds to ["INBOX"] filters notifications strictly to new messages arriving in the primary inbox. [1] (https://kb.torq.io/en/articles/9138324-receive-gmail-push-notifications-using-google-cloud-pub-sub), [2] (https://medium.com/@eagnir/understanding-gmails-push-notifications-via-google-cloud-pub-sub-3a002f9350ef)

Important Maintenance Requirements

• The 7-Day Expiration: Gmail watch requests automatically expire after 7 days. You must set up a cron job or a Cloud Scheduler process to call the users.watch endpoint automatically every 3 to 5 days to reset the timer. [1] (https://www.youtube.com/watch?v=f6Rbtur4JLc), [2] (https://kb.torq.io/en/articles/9138324-receive-gmail-push-notifications-using-google-cloud-pub-sub)
• Handling the Payload: Pub/Sub notifications do not contain the email body or subject. They only contain a historyId and emailAddress. When your application receives a notification, it must use that historyId to call the Gmail API's users.history.list or users.messages.get endpoint to download the actual email data. [1] (https://discuss.google.dev/t/pub-sub-subscription-filter-for-gmail-publisher/126229), [2] (https://medium.com/@eagnir/understanding-gmails-push-notifications-via-google-cloud-pub-sub-3a002f9350ef), [3] (https://www.youtube.com/watch?v=cgxQPUyZAXk&t=40)
For complete environment configurations and architecture diagrams, check the official Gmail API Push Notifications Guide or read Nirav Shah's breakdown on Medium regarding history logging. [1] (https://developers.google.com/workspace/gmail/api/guides/push), [2] (https://medium.com/@eagnir/understanding-gmails-push-notifications-via-google-cloud-pub-sub-3a002f9350ef)
If you'd like, let me know:
• What backend language (Python, Node.js, etc.) you plan to use for your subscriber application.
• Whether you want a Push subscription (Webhooks) or a Pull subscription.
I can provide the exact code block to handle the watch renewal and payload decoding!
