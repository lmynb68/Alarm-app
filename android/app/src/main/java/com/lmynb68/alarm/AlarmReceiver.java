package com.lmynb68.alarm;
import android.content.*;
import java.time.ZoneId;
public final class AlarmReceiver extends BroadcastReceiver {
 @Override public void onReceive(Context c,Intent i){
  String id=i.getStringExtra("id");boolean snooze=i.getBooleanExtra("snooze",false);long due=i.getLongExtra("due",0);if(id==null||due==0)return;
  try{synchronized(Store.class){
   Store.Data d=Store.load(c);Alarm a=Store.find(d,id);if(a==null)return;
   if(snooze){if(a.snooze!=due)return;a.snooze=0;}
   else {
    if(!a.enabled||a.due!=due)return;
    long now=System.currentTimeMillis();Store.count(d,id);
    if(a.interval){TimeRules.IntervalResult result=TimeRules.advanceInterval(a.fired,a.total,due,now,a.minutes);a.fired=result.fired;a.enabled=result.enabled;a.due=result.due;if(!a.enabled)Store.close(d,id,"Usage",now);}
    else if(a.days==0){a.enabled=false;a.due=0;Store.close(d,id,"Usage",now);}else a.due=TimeRules.next(now,a.hour,a.minute,a.second,a.days,ZoneId.systemDefault());
   }
   Store.save(c,d);
   Intent service=new Intent(c,RingService.class).putExtra("id",id).putExtra("token",id+":"+due+":"+snooze);
   c.startForegroundService(service);
   if(Scheduler.allowed(c))Scheduler.schedule(c,a);
  }}catch(Exception e){Errors.notify(c,e);}
 }
}
