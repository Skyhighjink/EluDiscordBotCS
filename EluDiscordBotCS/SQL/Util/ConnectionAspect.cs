using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Text;

namespace EluDiscordBotCS.SQL.Util
{
  [Serializable]
  public class ConnectionAspect : OnMethodBoundaryAspect
  {
    public override void OnEntry(MethodExecutionArgs args)
    {
      if(((ELUSQLInterface)args.Instance).m_Conn.State == System.Data.ConnectionState.Closed)
        ((ELUSQLInterface)args.Instance).m_Conn.Open();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
      if (((ELUSQLInterface)args.Instance).m_Conn.State == System.Data.ConnectionState.Open)
        ((ELUSQLInterface)args.Instance).m_Conn.Close();
    }

    public override void OnException(MethodExecutionArgs args)
    {
      OnExit(args);
    }
  }
}
