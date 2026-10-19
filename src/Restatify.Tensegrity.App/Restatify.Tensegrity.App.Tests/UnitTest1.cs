using NUnit.Framework;
using Restatify.Tensegrity.App.Core;

namespace Restatify.Tensegrity.App.Tests;

public class UnitTest1
{
    [Test]
    public void Offen_IsCloudAllowed()
    {
        Assert.That(ConfidentialityPolicy.IsCloudAllowed(ConfidentialityLevel.Offen), Is.True);
    }

    [Test]
    public void Vertraulich_IsCloudAllowed()
    {
        Assert.That(ConfidentialityPolicy.IsCloudAllowed(ConfidentialityLevel.Vertraulich), Is.True);
    }

    [Test]
    public void StrengGeheim_IsNotCloudAllowed()
    {
        Assert.That(ConfidentialityPolicy.IsCloudAllowed(ConfidentialityLevel.StrengGeheim), Is.False);
    }

    [Test]
    public void NurLokal_IsNotCloudAllowed()
    {
        Assert.That(ConfidentialityPolicy.IsCloudAllowed(ConfidentialityLevel.NurLokal), Is.False);
    }

    [Test]
    public void StrengGeheim_IsLocalOnly()
    {
        Assert.That(ConfidentialityPolicy.IsLocalOnly(ConfidentialityLevel.StrengGeheim), Is.False);
    }

    [Test]
    public void NurLokal_IsLocalOnly()
    {
        Assert.That(ConfidentialityPolicy.IsLocalOnly(ConfidentialityLevel.NurLokal), Is.True);
    }

    [Test]
    public void StrengGeheim_IsBlockedInWeb()
    {
        Assert.That(ConfidentialityPolicy.IsBlockedInWeb(ConfidentialityLevel.StrengGeheim), Is.True);
    }

    [Test]
    public void NurLokal_IsBlockedInWeb()
    {
        Assert.That(ConfidentialityPolicy.IsBlockedInWeb(ConfidentialityLevel.NurLokal), Is.True);
    }

    [Test]
    public void Offen_IsNotBlockedInWeb()
    {
        Assert.That(ConfidentialityPolicy.IsBlockedInWeb(ConfidentialityLevel.Offen), Is.False);
    }

    [Test]
    public void Vertraulich_IsNotBlockedInWeb()
    {
        Assert.That(ConfidentialityPolicy.IsBlockedInWeb(ConfidentialityLevel.Vertraulich), Is.False);
    }

    [Test]
    public void TaintMax_Returns_Strictest_Level()
    {
        var levels = new[]
        {
            ConfidentialityLevel.Offen,
            ConfidentialityLevel.Vertraulich,
            ConfidentialityLevel.StrengGeheim
        };

        var result = ConfidentialityPolicy.TaintMax(levels);

        Assert.That(result, Is.EqualTo(ConfidentialityLevel.StrengGeheim));
    }

    [Test]
    public void TaintMax_Returns_Offen_When_Empty()
    {
        var result = ConfidentialityPolicy.TaintMax(Array.Empty<ConfidentialityLevel>());

        Assert.That(result, Is.EqualTo(ConfidentialityLevel.Offen));
    }

    [Test]
    public void TaintMax_Returns_Offen_When_Null()
    {
        var result = ConfidentialityPolicy.TaintMax(null!);

        Assert.That(result, Is.EqualTo(ConfidentialityLevel.Offen));
    }

    [Test]
    public void CloudRequestBuilder_Builds_Request_For_Allowed_Levels()
    {
        var result = CloudRequestBuilder.TryBuild("req-1", ConfidentialityLevel.Offen, "Hallo", new[] { "legal", " legal ", "" });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ExpertSlotIds, Is.EqualTo(new[] { "legal" }));
    }

    [Test]
    public void CloudRequestBuilder_Returns_Null_For_Blocked_Levels()
    {
        Assert.That(CloudRequestBuilder.TryBuild("req-2", ConfidentialityLevel.StrengGeheim, "Geheim", null), Is.Null);
        Assert.That(CloudRequestBuilder.TryBuild("req-3", ConfidentialityLevel.NurLokal, "Lokal", null), Is.Null);
    }

    [Test]
    public void CloudRequestBuilder_Maps_Levels_To_Cloud_Names()
    {
        Assert.That(CloudRequestBuilder.ToCloudLevelName(ConfidentialityLevel.Offen), Is.EqualTo("Offen"));
        Assert.That(CloudRequestBuilder.ToCloudLevelName(ConfidentialityLevel.Vertraulich), Is.EqualTo("Vertraulich"));
        Assert.That(CloudRequestBuilder.ToCloudLevelName(ConfidentialityLevel.NurLokal), Is.EqualTo("Offen"));
    }

    [Test]
    public void Cloud_Dtos_Expose_All_Properties()
    {
        var request = new GenerateRequest("req-9", ConfidentialityLevel.Vertraulich, "Frage", new[] { "legal" });
        var result = new GenerateResult(true, "generalist", new[] { "legal" });

        Assert.That(request.RequestId, Is.EqualTo("req-9"));
        Assert.That(request.Level, Is.EqualTo(ConfidentialityLevel.Vertraulich));
        Assert.That(request.Prompt, Is.EqualTo("Frage"));
        Assert.That(request.ExpertSlotIds, Is.EqualTo(new[] { "legal" }));
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.Route, Is.EqualTo("generalist"));
        Assert.That(result.SelectedExpertSlotIds, Is.EqualTo(new[] { "legal" }));
    }

    [Test]
    public async Task ViewModel_SendAsync_Uses_Gateway_For_Allowed_Level()
    {
        var gateway = new FakeGateway();
        var viewModel = new MainViewModel(gateway)
        {
            SelectedLevel = ConfidentialityLevel.Offen,
            Prompt = "Frage"
        };

        await viewModel.SendAsync();

        Assert.That(gateway.CallCount, Is.EqualTo(1));
        Assert.That(viewModel.StatusText, Does.Contain("Antwort"));
    }

    [Test]
    public async Task ViewModel_SendAsync_Blocks_Restricted_Level_Without_Calling_Gateway()
    {
        var gateway = new FakeGateway();
        var viewModel = new MainViewModel(gateway)
        {
            SelectedLevel = ConfidentialityLevel.NurLokal,
            Prompt = "Geheim"
        };

        await viewModel.SendAsync();

        Assert.That(gateway.CallCount, Is.EqualTo(0));
        Assert.That(viewModel.StatusText, Does.Contain("Blockiert"));
    }

    [Test]
    public async Task ViewModel_SendAsync_Appends_User_And_Assistant_Messages()
    {
        var gateway = new FakeGateway();
        var viewModel = new MainViewModel(gateway)
        {
            SelectedLevel = ConfidentialityLevel.Offen,
            Prompt = "Frage"
        };
        var before = viewModel.Messages.Count;

        await viewModel.SendAsync();

        Assert.That(viewModel.Messages.Count, Is.EqualTo(before + 2));
        Assert.That(viewModel.Messages[^2].IsUser, Is.True);
        Assert.That(viewModel.Messages[^2].Text, Is.EqualTo("Frage"));
        Assert.That(viewModel.Messages[^1].IsUser, Is.False);
        Assert.That(viewModel.Prompt, Is.Empty);
    }

    [Test]
    public async Task ViewModel_SendAsync_Blocked_Adds_Assistant_Note_Without_User_Message()
    {
        var gateway = new FakeGateway();
        var viewModel = new MainViewModel(gateway)
        {
            SelectedLevel = ConfidentialityLevel.NurLokal,
            Prompt = "Geheim"
        };
        var before = viewModel.Messages.Count;

        await viewModel.SendAsync();

        Assert.That(viewModel.Messages.Count, Is.EqualTo(before + 1));
        Assert.That(viewModel.Messages[^1].IsUser, Is.False);
        Assert.That(gateway.CallCount, Is.EqualTo(0));
    }

    [Test]
    public void ViewModel_Seeds_Welcome_Message_And_Privacy_Mode_Text()
    {
        var viewModel = new MainViewModel(new FakeGateway());

        Assert.That(viewModel.Messages, Is.Not.Empty);
        Assert.That(viewModel.Messages[0].IsUser, Is.False);
        Assert.That(viewModel.PrivacyModeText, Is.EqualTo("Open"));

        viewModel.SelectedLevel = ConfidentialityLevel.StrengGeheim;
        Assert.That(viewModel.PrivacyModeText, Is.EqualTo("Strictly confidential"));
    }

    [Test]
    public void ViewModel_CanSend_Requires_Prompt_And_Allowed_Level()
    {
        var viewModel = new MainViewModel(new FakeGateway());

        Assert.That(viewModel.CanSend, Is.False);
        viewModel.Prompt = "Frage";
        Assert.That(viewModel.CanSend, Is.True);
        viewModel.SelectedLevel = ConfidentialityLevel.StrengGeheim;
        Assert.That(viewModel.CanSend, Is.False);
    }

    private sealed class FakeGateway : ICloudGateway
    {
        public int CallCount { get; private set; }

        public Task<GenerateResult> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new GenerateResult(true, "generalist", Array.Empty<string>()));
        }
    }

    [Test]
    public void Feedback_With_Evidence_And_Valid_Rating_Is_Accepted()
    {
        var draft = new FeedbackDraft(
            "evt-1",
            "app-1",
            "model-1",
            1,
            "Hilfreich",
            ConfidentialityLevel.Offen,
            new[] { new EvidenceDraft("src-1", "Quelle A", DateTimeOffset.UtcNow) });

        var (accepted, _) = FeedbackValidator.Validate(draft);

        Assert.That(accepted, Is.True);
    }

    [Test]
    public void Feedback_Without_Evidence_Is_Rejected_When_Rated()
    {
        var draft = new FeedbackDraft("evt-2", "app-1", "model-1", -1, "Falsch", ConfidentialityLevel.Offen);

        var (accepted, reason) = FeedbackValidator.Validate(draft);

        Assert.That(accepted, Is.False);
        Assert.That(reason, Does.Contain("Beleg"));
    }

    [Test]
    public void Feedback_With_Local_Only_Level_Is_Rejected()
    {
        var draft = new FeedbackDraft("evt-3", "app-1", "model-1", 0, "Notiz", ConfidentialityLevel.NurLokal);

        var (accepted, reason) = FeedbackValidator.Validate(draft);

        Assert.That(accepted, Is.False);
        Assert.That(reason, Does.Contain("NUR_LOKAL"));
    }

    [Test]
    public void Feedback_With_Invalid_Rating_Is_Rejected()
    {
        var draft = new FeedbackDraft("evt-4", "app-1", "model-1", 5, "Ungültig", ConfidentialityLevel.Offen);

        var (accepted, _) = FeedbackValidator.Validate(draft);

        Assert.That(accepted, Is.False);
    }

    [Test]
    public void Feedback_With_Missing_Identifiers_Is_Rejected()
    {
        var draft = new FeedbackDraft("", "app-1", "model-1", 0, "Kommentar", ConfidentialityLevel.Offen);

        var (accepted, _) = FeedbackValidator.Validate(draft);

        Assert.That(accepted, Is.False);
    }

    [Test]
    public void Feedback_CanSubmit_Requires_Rating_And_Citation()
    {
        var viewModel = new MainViewModel(new FakeGateway());
        var feedback = viewModel.Feedback;

        Assert.That(feedback.CanSubmit, Is.False);

        feedback.Rating = 1;
        Assert.That(feedback.CanSubmit, Is.False);

        feedback.Citation = "Quelle";
        Assert.That(feedback.CanSubmit, Is.True);
    }

    [Test]
    public async Task Feedback_Submit_Blocks_Local_Only_Level()
    {
        var viewModel = new MainViewModel(new FakeGateway());
        viewModel.SelectedLevel = ConfidentialityLevel.NurLokal;
        var feedback = viewModel.Feedback;
        feedback.Rating = 1;
        feedback.Citation = "Quelle";

        var accepted = await feedback.SubmitAsync();

        Assert.That(accepted, Is.False);
        Assert.That(feedback.StatusText, Does.Contain("verworfen"));
    }

    [Test]
    public async Task Feedback_Submit_Accepts_Valid_Feedback()
    {
        var viewModel = new MainViewModel(new FakeGateway());
        viewModel.SelectedLevel = ConfidentialityLevel.Offen;
        var feedback = viewModel.Feedback;
        feedback.Rating = 1;
        feedback.Citation = "Quelle";

        var accepted = await feedback.SubmitAsync();

        Assert.That(accepted, Is.True);
        Assert.That(feedback.StatusText, Does.Contain("angenommen"));
    }

    [Test]
    public async Task Feedback_SubmitAsync_Sends_Draft_To_Channel()
    {
        var channel = new FakeFeedbackChannel();
        var viewModel = new MainViewModel(new FakeGateway());
        var feedback = new FeedbackViewModel(viewModel, channel)
        {
            Rating = 1,
            Citation = "Quelle"
        };

        var accepted = await feedback.SubmitAsync();

        Assert.That(accepted, Is.True);
        Assert.That(channel.CallCount, Is.EqualTo(1));
        Assert.That(channel.LastDraft, Is.Not.Null);
        Assert.That(channel.LastDraft!.Rating, Is.EqualTo(1));
    }

    private sealed class FakeFeedbackChannel : IFeedbackChannel
    {
        public int CallCount { get; private set; }
        public FeedbackDraft? LastDraft { get; private set; }

        public Task<FeedbackAck> SubmitAsync(FeedbackDraft draft, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastDraft = draft;
            return Task.FromResult(new FeedbackAck(true, "accepted"));
        }
    }
}

