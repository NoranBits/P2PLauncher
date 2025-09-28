namespace P2PLauncher.Client.Services;

public interface IFeedbackService
{
    Task AppendLogLineAsync(string message);
    Task AppendLogLineAsync(string message, CancellationToken cancellationToken = default);
    IObservable<string> LogLines { get; }
}