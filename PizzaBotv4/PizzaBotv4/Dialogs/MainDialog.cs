// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.10.3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

using PizzaBotv4;
using PizzaBotv4.CognitiveModels;

namespace PizzaBotv4.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly PizzaOrderRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(PizzaOrderRecognizer luisRecognizer, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        public static string SSML(string text)
        {
            
            text = text.Trim( new Char[] { '\\'});
            //todo replace double quotes with singe quotes
            text = text.Replace("\"", "'");  
            text = text.Replace("\n", String.Empty);
            //string ssml =  @"<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='Microsoft Server Speech Text to Speech Voice (en-GB, MiaNeural'>" + $"{text}" + "</voice></speak>";

            string ssml = $@"<speak xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xmlns:emo='http://www.w3.org/2009/10/emotionml' version='1.0' xml:lang='en-GB'><voice name='Microsoft Server Speech Text to Speech Voice (en-GB, MiaNeural)'>{text}</voice></speak>";
            return ssml;
            
            
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?\nSay something like \"I'd like to order one large pepperoni pizza with extra mushrooms!\"";
            var promptMessage = MessageFactory.Text(messageText, SSML(messageText), InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }


            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)

            var luisResult = await _luisRecognizer.RecognizeAsync<PizzaOrder>(stepContext.Context, cancellationToken);

            switch (luisResult.TopIntent().intent)
            {
                case PizzaOrder.Intent.ModifyOrder:
                    
                    var pizzaOrder = luisResult;
                    var pizzaResponseText = PizzaOrder.GetPizzaOrderString(pizzaOrder);
                    /*
                    var responseCardAttachment = new HeroCard("Your order has been placed! ", null, pizzaResponseText, new List<CardImage>() { new CardImage(PizzaOrder.GetPizzaImage()) }).ToAttachment();
                    var chatActivity = Activity.CreateMessageActivity();
                    chatActivity.Attachments.Add(responseCardAttachment);
                    await stepContext.Context.SendActivityAsync(chatActivity);
                    */

                    var responseCardAttachment = new HeroCard
                    {
                        Title = "Your order has been placed!",
                        Subtitle = "",
                        Text = pizzaResponseText,
                        // Images = new List<CardImage> {new CardImage(PizzaOrder.GetPizzaImage() )},
                        Images = new List<CardImage> { new CardImage("https://images.pexels.com/photos/1049626/pexels-photo-1049626.jpeg?auto=compress&cs=tinysrgb&h=350") },
                        //Images = null,
                        Buttons = null,
                    };

                    var reply = MessageFactory.Attachment(responseCardAttachment.ToAttachment());
                    reply.Speak = SSML(PizzaOrder.GetPizzaOrderStringTextOnly(pizzaOrder));
                    await stepContext.Context.SendActivityAsync(reply, cancellationToken);

                    break;

                case PizzaOrder.Intent.Confirmation:
                    var confirmationMessage = MessageFactory.Text("That's great! We'll get right on it", null, InputHints.IgnoringInput);
                    confirmationMessage.Speak = SSML(confirmationMessage.Text);
                    await stepContext.Context.SendActivityAsync(confirmationMessage, cancellationToken);
                    break;

                case PizzaOrder.Intent.CancelOrder:
                    var cancelMessage = MessageFactory.Text("Your order has been cancelled!", null, InputHints.IgnoringInput);
                    cancelMessage.Speak = SSML(cancelMessage.Text);
                    await stepContext.Context.SendActivityAsync(cancelMessage, cancellationToken);
                    break;

                case PizzaOrder.Intent.Greetings:
                    var greetingsMessage = MessageFactory.Text("Hello!", null, InputHints.IgnoringInput);
                    greetingsMessage.Speak = SSML(greetingsMessage.Text);
                    await stepContext.Context.SendActivityAsync(greetingsMessage, cancellationToken);
                    break;

                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, SSML(didntUnderstandMessageText), InputHints.IgnoringInput);
                    didntUnderstandMessage.Speak = SSML(didntUnderstandMessageText);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            //if (stepContext.Result is BookingDetails result)
            //{
            //    // Now we have all the booking details call the booking service.

            //    // If the call to the booking service was successful tell the user.

            //    var timeProperty = new TimexProperty(result.TravelDate);
            //    var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
            //    var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
            //    var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            //    await stepContext.Context.SendActivityAsync(message, cancellationToken);
            //}

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
