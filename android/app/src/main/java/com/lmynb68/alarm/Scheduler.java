package com.lmynb68.alarm;
import android.app.*;
import android.content.*;
import android.net.Uri;
import android.os.Build;
import java.time.ZoneId;
final class Scheduler {
 static boolean allowed(Context c){return Build.VERSION.SDK_INT<31||c.getSystemService(AlarmManager.class).canScheduleExactAlarms();}
 static PendingIntent trigger(Context c,Alarm a,boolean snooze,long time){
  Intent i=new Intent(c,AlarmReceiver.class).setData(Uri.parse("alarmapp://"+(snooze?"snooze":"alarm")+"/"+a.id)).putExtra("id",a.id).putExtra("due",time).putExtra("snooze",snooze);
  return PendingIntent.getBroadcast(c,0,i,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
 }
 static void cancel(Context c,Alarm a){AlarmManager m=c.getSystemService(AlarmManager.class);m.cancel(trigger(c,a,false,0));m.cancel(trigger(c,a,true,0));}
 // Permission is checked by allowed() immediately before scheduling.
 @android.annotation.SuppressLint("ScheduleExactAlarm")
 static void schedule(Context c,Alarm a){
  if(!a.enabled&&a.snooze<=0)return;
  if(!allowed(c))throw new SecurityException("Exact alarm permission required");
  AlarmManager m=c.getSystemService(AlarmManager.class);
  PendingIntent show=PendingIntent.getActivity(c,0,new Intent(c,MainActivity.class),PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
  if(a.enabled&&a.due>0)m.setAlarmClock(new AlarmManager.AlarmClockInfo(Math.max(System.currentTimeMillis()+250,a.due),show),trigger(c,a,false,a.due));
  if(a.snooze>0)m.setAlarmClock(new AlarmManager.AlarmClockInfo(Math.max(System.currentTimeMillis()+250,a.snooze),show),trigger(c,a,true,a.snooze));
 }
 static void restart(Alarm a,long now){a.fired=0;a.snooze=0;a.due=a.interval?now+a.minutes*60000L:TimeRules.next(now,a.hour,a.minute,a.second,a.days,ZoneId.systemDefault());}
 static void restore(Context c,boolean resetClock){
  synchronized(Store.class){Store.Data d=Store.load(c);long now=System.currentTimeMillis();
   for(Alarm a:d.alarms){
    cancel(c,a);
    if(a.enabled){if(a.due<=0 || resetClock&&(!a.interval||a.due<=now)){a.due=a.interval?now+a.minutes*60000L:TimeRules.next(now,a.hour,a.minute,a.second,a.days,ZoneId.systemDefault());}}
    if(a.snooze>0&&a.snooze<=now)a.snooze=now+1000;
   }
   Store.save(c,d);if(allowed(c))for(Alarm a:d.alarms)schedule(c,a);
  }
 }
}
