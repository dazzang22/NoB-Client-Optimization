using NUnit.Framework;

public class TargetSelectionPolicyTests
{
    [Test]
    public void MoreCenteredCandidate_WinsEvenWhenFarther()
    {
        bool result = TargetSelectionPolicy.IsBetterCandidate(
            alignment: 0.95f,
            sqrDistance: 1.44f,
            bestAlignment: 0.80f,
            bestSqrDistance: 0.64f,
            hasBestCandidate: true);

        Assert.IsTrue(result);
    }

    [Test]
    public void SimilarAlignment_ChoosesCloserCandidate()
    {
        bool result = TargetSelectionPolicy.IsBetterCandidate(
            alignment: 0.89f,
            sqrDistance: 0.64f,
            bestAlignment: 0.90f,
            bestSqrDistance: 1.44f,
            hasBestCandidate: true);

        Assert.IsTrue(result);
    }

    [Test]
    public void LessCenteredCandidate_DoesNotWinJustBecauseItIsCloser()
    {
        bool result = TargetSelectionPolicy.IsBetterCandidate(
            alignment: 0.70f,
            sqrDistance: 0.25f,
            bestAlignment: 0.90f,
            bestSqrDistance: 1.44f,
            hasBestCandidate: true);

        Assert.IsFalse(result);
    }
}
