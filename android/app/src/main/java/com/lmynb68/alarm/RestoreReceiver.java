package com.lmynb68.alarm;
import android.content.*;
public final class RestoreReceiver extends BroadcastReceiver {
 @Override public void onReceive(Context c,Intent i){
  if(i==null)return;
  String action=i.getAction();
  if(!Intent.ACTION_BOOT_COMPLETED.equals(action) && !Intent.ACTION_MY_PACKAGE_REPLACED.equals(action) && !Intent.ACTION_TIME_CHANGED.equals(action) && !Intent.ACTION_TIMEZONE_CHANGED.equals(action) && !"android.app.action.SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED".equals(action))return;
  try{Scheduler.restore(c,true);}catch(Exception e){Errors.notify(c,e);}
 }
}
