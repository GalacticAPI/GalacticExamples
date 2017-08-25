using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
using Galactic.FileSystem;
using Galactic.PowerShell;

namespace PowerShellExample
{
    class Program
    {
        // ---------- CONSTANTS ----------
        // State values are passed through to a callback function that handles scripts / commands that are run asynchronously. (See example below.)

        // State value names:

        //The name of a state value containing the invocation time of a script / command run asynchronously.
        const string STATE_VALUE_NAME_INVOCATION_TIME = "invocationTime";

        // Script command-line parameter names:

        // The name of the text value to supply the sample script below.
        const string PARAMETER_NAME_TEXT = "text";

        // The number of scripts to run in parallel.
        const int NUM_SCRIPTS_TO_RUN = 20;

        // ---------- METHODS ----------

        static void Main(string[] args)
        {

            // ---------- SYNCHRONOUS RUN EXAMPLE ----------

            Console.WriteLine("Starting synchronous script run.");

            // This creates a runspace for you to run your scripts within. It's basically a thread in the current process and can be reused if you'd like to run multiple scripts within the same thread.
            System.Management.Automation.Runspaces.Runspace runspace = System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspace();

            // These are the PowerShell objects that are returned from a script run (if any).
            Collection<System.Management.Automation.PSObject> results = new Collection<System.Management.Automation.PSObject>();

            // Runs the script synchronously, and deposits PowerShell objects of the results in psObjects.
            results = PowerShell.RunSynchronously(@"gci C:\", ref runspace);

            if (results != null)
            {
                foreach (System.Management.Automation.PSObject result in results)
                {
                    Console.WriteLine("Name: " + result.Properties["Name"].Value + " | CreationTime: " + result.Properties["CreationTime"].Value);
                }
            }

            // Clean up the runspace.
            runspace.Dispose();

            Console.WriteLine("Synchronous script run complete.");
            Console.WriteLine();

            // ---------- ASYNCHRONOUS RUN EXAMPLE ----------
            // This example will show how to run multiple PowerShell scripts simultaneously.
            // Also it shows that you can read script text from a file and run it.
            // It also demonstrates how to supply command-line parameters to scripts.

            Console.WriteLine("Starting asynchronous script run.");

            // Load the contents of the script file we want to run from the filesystem.
            string scriptContents = File.ReadAllAsText("testScript.ps1");

            // Check that we were able to read the contents of the script.
            if (!string.IsNullOrWhiteSpace(scriptContents))
            {

                // This creates a runspace pool, basically a collection of runspaces for running multiple threads with scripts simultaneously.
                // It creates NUM_SCRIPTS_TO_RUN threads so each script can run parallel to the other ones.
                System.Management.Automation.Runspaces.RunspacePool runspacePool = System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspacePool(1, NUM_SCRIPTS_TO_RUN);

                // A list of handles that can be used to wait for the results of script runs.
                List<WaitHandle> waitHandles = new List<WaitHandle>();

                // We're going to start 20 threads of PowerShell each running a seperate command / script.
                for (int i = 1; i <= NUM_SCRIPTS_TO_RUN; i++)
                {
                    // Also you can pass variables through, for use by the callback function that handles the results. These are called stateValues.
                    // Here's we'll pass the time that the script was invoked.
                    Dictionary<string, Object> stateValues = new Dictionary<string, object>();
                    stateValues.Add(STATE_VALUE_NAME_INVOCATION_TIME, DateTime.Now);

                    // Creates a list of command line parameters to supply the script.
                    KeyValuePair<string, object>[] parameters = new KeyValuePair<string, object>[1];

                    // Add a the text parameter to the list of parameters. Its value will be the number of the script run as specified by i.
                    parameters[0] = new KeyValuePair<string, object>(PARAMETER_NAME_TEXT, i);

                    // Runs the scripts asynchronously in their own threads. (Note: There is a random sleep of up to 5 seconds in the script to simulate variable runtimes.)
                    waitHandles.Add(PowerShell.RunAsynchronously(scriptContents, ref runspacePool, ProcessResults, null, null, stateValues, parameters));
                }

                // Wait until all scripts are complete.
                WaitHandle.WaitAll(waitHandles.ToArray());

                // Clean up the wait handles.
                foreach (WaitHandle waitHandle in waitHandles)
                {
                    waitHandle.Close();
                }

                // Clean up the runspace pool.
                runspacePool.Dispose();

                Console.WriteLine("Asynchronous script runs complete.");
                Console.WriteLine();
            }
            else
            {
                // Couldn't read the contents of the script from the filesystem.
                Console.WriteLine("Error: Couldn't read the contents of the script from the filesystem.");
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// A callback that processes the results of the asynchronous PowerShell script.
        /// </summary>
        /// <param name="results">The results of the script run.</param>
        /// <param name="stateValues">State values that are passed through the script invocation.</param>
        protected static void ProcessResults(System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> results, Dictionary<string, Object> stateValues)
        {
            // Check that the state values made it through the script run.
            if (stateValues != null && stateValues.Count > 0 &&
                stateValues.ContainsKey(STATE_VALUE_NAME_INVOCATION_TIME))
            {
                // Check that results were retuned by the script.
                if (results != null)
                {
                    // For our purposes this will only return a single result... but you should handle all possible results when used in your code.
                    foreach (System.Management.Automation.PSObject result in results)
                    {
                        // Get the time of invocation from the state value.
                        DateTime invocationTime = (DateTime)stateValues[STATE_VALUE_NAME_INVOCATION_TIME];

                        // We'll write the result of the script run, as well as the time that the script was invoked via the state values passed through to this callback.
                        Console.WriteLine("Script Completed: " + result + " | Invocation Time: " + invocationTime.ToLongTimeString() + " " + invocationTime.Millisecond + "ms");
                    }
                 }
            }
            else
            {
                // Indicate that the state values didn't make it through the script run.
                Console.WriteLine("Error: No state values were passed through the script.");
            }
        }
    }
}
