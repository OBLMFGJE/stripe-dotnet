namespace StripeTests
{
    using Stripe;
    using Xunit;

    public class StripeEventUtilityTest : BaseStripeTest
    {
        private int eventTimestamp;
        private string signature;
        private string json;
        private string secret;

        public StripeEventUtilityTest()
        {
            this.eventTimestamp = 1533204620;
            this.secret = "webhook_secret";
            this.signature = $"t={this.eventTimestamp},v1=cc2ebf93827f22fecfc48f8c665079f73cf0c10a86c16e2607f8be8bda1cfb14,v0=63f3a72374a733066c4be69ed7f8e5ac85c22c9f0a6a612ab9a025a9e4ee7eef";
            this.json = GetResourceAsString("event_test_signature.json");
        }

        [Fact]
        public void ConstructEvent()
        {
            var tolerance = 300;
            var fakeCurrentTimestamp = this.eventTimestamp + 100;
            var evt = StripeEventUtility.ConstructEvent(this.json, this.signature, this.secret, tolerance, fakeCurrentTimestamp);

            Assert.NotNull(evt);
            Assert.Equal("acct_123", evt.Account);
            Assert.Equal("req_123", evt.Request.Id);
            Assert.Equal("idempotency-key-123", evt.Request.IdempotencyKey);
        }

        [Fact]
        public void RejectOutdatedTimestamp()
        {
            var tolerance = 300;
            var fakeCurrentTimestamp = this.eventTimestamp + tolerance + 100;

            var exception = Assert.Throws<StripeException>(() =>
                StripeEventUtility.ConstructEvent(this.json, this.signature, this.secret, tolerance, fakeCurrentTimestamp));

            Assert.Equal("The webhook cannot be processed because the current timestamp is outside of the allowed tolerance.", exception.Message);
        }

        [Fact]
        public void RejectIncorrectSignature()
        {
            // This throws an error because the original JSON message is modified
            var exception = Assert.Throws<StripeException>(() =>
                StripeEventUtility.ConstructEvent(this.json + "this_changes_the_json", this.signature, this.secret));

            Assert.Equal("The signature for the webhook is not present in the Stripe-Signature header.", exception.Message);
        }

        [Fact]
        public void RejectSecretWithUnicode()
        {
            var exception = Assert.Throws<StripeException>(() =>
                StripeEventUtility.ConstructEvent(this.json, this.signature, this.secret + "\ud802"));

            Assert.Equal("The webhook cannot be processed because the signature cannot be safely calculated.", exception.Message);
        }

        [Fact]
        public void RejectMessageWithUnicode()
        {
            var exception = Assert.Throws<StripeException>(() =>
                StripeEventUtility.ConstructEvent(this.json + "\ud802", this.signature, this.secret));

            Assert.Equal("The webhook cannot be processed because the signature cannot be safely calculated.", exception.Message);
        }
    }
}
