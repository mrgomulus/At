namespace OBD2Suite.Models
{
    public enum OnboardTestResult { Pass, Fail, NotCompleted }

    /// <summary>
    /// OBD2 Mode 06 on-board monitoring test result for a specific sub-test.
    /// </summary>
    public class OnboardTest
    {
        public string TestId { get; set; } = "";        // e.g. "$01" catalyst monitor bank 1
        public string ComponentId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double MeasuredValue { get; set; }
        public double MinLimit { get; set; }
        public double MaxLimit { get; set; }
        public string Unit { get; set; } = "";
        public OnboardTestResult Result { get; set; } = OnboardTestResult.NotCompleted;

        public string ResultText => Result switch
        {
            OnboardTestResult.Pass         => "✔ Pass",
            OnboardTestResult.Fail         => "✖ FAIL",
            OnboardTestResult.NotCompleted => "— Not Run",
            _ => ""
        };

        public string ResultColor => Result switch
        {
            OnboardTestResult.Pass         => "#4CAF50",
            OnboardTestResult.Fail         => "#FF5252",
            _ => "#888888"
        };

        public bool IsWithinLimits => MeasuredValue >= MinLimit && MeasuredValue <= MaxLimit;
    }
}
