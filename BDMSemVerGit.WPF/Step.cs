using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDMSemVerGit.WPF
{
	public class Step
	{
		public Int32 Number { get; set; }
		public String Name { get; set; }
		public Boolean IsComplete { get; set; }
		public StepStateData BeforeWork { get; set; }
		public StepStateData AfterWork { get; set; }

		public Action DoWorkCallBack { get; set; }
		public Action BeforePreviousCallBack { get; set; }
		public Action BeforeNextCallBack { get; set; }

		public override String ToString()
		{
			return $"{this.Number}: {this.Name}";
		}
	}

	public class StepStateData
	{
		public String Description { get; set; }
		public Object Content { get; set; }
		public String Instructions { get; set; }
		public StepButtonState PreviousState { get; set; }
		public StepButtonState NextState { get; set; }
		public StepButtonState CloseState { get; set; }
		public Action CallBack { get; set; }
	}

	public class StepButtonState
	{
		//public String Name { get; set; }
		public Boolean IsVisibile { get; set; }
		public Boolean IsEnabled { get; set; }

		public static StepButtonState NotVisibileNotEnabled => new()
		{
			IsVisibile = false,
			IsEnabled = false
		};
		public static StepButtonState VisibileEnabled => new()
		{
			IsVisibile = true,
			IsEnabled = true
		};
		public static StepButtonState VisibileNotEnabled => new()
		{
			IsVisibile = true,
			IsEnabled = false
		};
	}
}
