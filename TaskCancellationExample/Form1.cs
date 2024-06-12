using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace TaskCancellationExample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            progressManager.ProgressChanged += progressManager_ProgressChanged; //Set the Progress changed event
        }

        Progress<string> progressManager = new Progress<string>();
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;

        void progressManager_ProgressChanged(object sender, string e)
        {
            lblStatus.Text = e;
        }

        private async void RunTask()
        {
            int numericValue = (int)numericUpDown1.Value; //Capture the user input
            object[] arrObjects = new object[] { numericValue };//Declare the array of objects

            //Because cancellation tokens cannot be reused after they have been canceled,
            //we need to create a new cancellation token before each start
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            bool cancelled = false;

            lblStatus.Text = "Started Calculation...";//Set the status label to signal
                                                      //Starting the operation
            btnStart.Enabled = false;//Disable the start button
            
            using (Task<string> task = new Task<string>(new Func<object, string>(PerformTaskAction), arrObjects, cancellationToken))//Declare and
            {
                lblStatus.Text = "Started Calculation...";//Set the status label to signal
                //Starting the operation
                btnStart.Enabled = false;//Disable the start button
                task.Start();//Start the execution of the task;
                try
                {
                    await task; //wait for the task to finish, without blocking the main thread
                }
                catch (OperationCanceledException ex)
                {
                    textBox1.Text = task.Result.ToString();
                    lblStatus.Text = ex.Message;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                
                if (!task.IsFaulted)
                {
                    textBox1.Text = task.Result.ToString();//at this point,
                                                           // the task has finished its background work, and we can take the result
                    if (cancellationToken.IsCancellationRequested)
                    {
                        lblStatus.Text = "Cancelled.";
                    }
                    else
                    {
                        lblStatus.Text = "Completed."; // Signal the completion of the task
                    }
                    
                }
                btnStart.Enabled = true; //Re-enable the Start button
            }
        }

        private int PerformHeavyOperation(int i)
        {
            //This simulates a heavy operation such as a call to a remote server,
            // requesting data from a database, complex operation, etc...
            System.Threading.Thread.Sleep(100);
            return i * 100;
        }

        //This is the method that will be executed by the task in the background thread...
        private string PerformTaskAction(object state)
        {
            object[] arrObjects = (object[])state; //Get the array of objects from the main thread
            int maxValue = (int)arrObjects[0]; //Get the maxValue integer from the array of objects

            StringBuilder sb = new StringBuilder();//Declare a new string builder to build the result
            for(int i = 0; i < maxValue; i++)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    sb.Append("Operation Cancelled.");
                    //cancellationTokenSource.Cancel();
                    break;
                    //break;
                }
                else
                {
                    sb.Append(string.Format("Counting Number {0}{1}", PerformHeavyOperation(i), Environment.NewLine));
                    //Append the result to the string builder

                    ((IProgress<string>)progressManager).Report(string.Format("Now Counting number: {0}...", i));//Report our progress to the main thread
                }
            }
            return sb.ToString(); //return the result
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RunTask();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
        }
    }//End Form1
}//End namespace TaskCancellationExample
