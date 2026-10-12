// Authored validation entry over the genuine Server and original Go host controls.
namespace LanguageServices.Checks.Fourslash;
public static class FourslashCandidateBackend
{
    public static int Run(string hostname, int port, CancellationToken cancellation)
    {
        Exception? error = CandidateHost.Run(hostname, port, cancellation);
        if (error is null)
            return 0;
        Console.Error.WriteLine(error);
        return 1;
    }
}
