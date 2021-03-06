using System.Workflow.ComponentModel;
using System.Workflow.Runtime;
using Composite.C1Console.Actions;
using Composite.C1Console.Events;


namespace Composite.C1Console.Workflow.Activities
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
    public sealed class CloseCurrentViewActivity : Activity
    {
        /// <exclude />
        public CloseCurrentViewActivity()
        {
        }



        /// <exclude />
        public CloseCurrentViewActivity(string name)
            : base(name)
        {
        }



        /// <exclude />
        protected sealed override ActivityExecutionStatus Execute(ActivityExecutionContext executionContext)
        {
            FormsWorkflow formsWorkflow = this.GetRoot<FormsWorkflow>();

            FlowControllerServicesContainer container = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
            if (container != null)
            {
                IManagementConsoleMessageService service = container.GetService<IManagementConsoleMessageService>();

                if (service != null)
                {
                    service.CloseCurrentView();
                }
            }

            return ActivityExecutionStatus.Closed;
        }
    }
}
