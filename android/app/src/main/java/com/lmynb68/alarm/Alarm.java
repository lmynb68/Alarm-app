package com.lmynb68.alarm;
import org.json.JSONObject;
import java.util.UUID;
public final class Alarm {
 public String id=UUID.randomUUID().toString(), name="Alarm", sound="classic";
 public boolean enabled=true, interval=false;
 public int hour=8, minute=0, second=0, days=0, minutes=5, total=12, fired=0;
 public long due=0, snooze=0;
 public JSONObject json() throws Exception {
  return new JSONObject().put("id",id).put("name",name).put("sound",sound).put("enabled",enabled).put("interval",interval).put("hour",hour).put("minute",minute).put("second",second).put("days",days).put("minutes",minutes).put("total",total).put("fired",fired).put("due",due).put("snooze",snooze);
 }
 public static Alarm read(JSONObject j) {
  Alarm a=new Alarm();a.id=j.optString("id",a.id);a.name=j.optString("name","Alarm");a.sound=j.optString("sound","classic");a.enabled=j.optBoolean("enabled");a.interval=j.optBoolean("interval");a.hour=j.optInt("hour",8);a.minute=j.optInt("minute");a.second=j.optInt("second");a.days=j.optInt("days");a.minutes=j.optInt("minutes",5);a.total=j.optInt("total",12);a.fired=j.optInt("fired");a.due=j.optLong("due");a.snooze=j.optLong("snooze");return a;
 }
 public String type(){return interval?"Interval":"Scheduled";}
}
