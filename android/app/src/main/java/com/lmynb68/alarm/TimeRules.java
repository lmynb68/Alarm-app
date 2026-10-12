package com.lmynb68.alarm;
import java.time.*;
public final class TimeRules {
 private TimeRules(){}
 public static long next(long now,int h,int m,int s,int mask,ZoneId zone) {
  ZonedDateTime current=Instant.ofEpochMilli(now).atZone(zone);
  for(int i=0;i<=7;i++){
   LocalDate date=current.toLocalDate().plusDays(i);
   int bit=date.getDayOfWeek().getValue()%7;
   ZonedDateTime candidate=ZonedDateTime.of(date,LocalTime.of(h,m,s),zone);
   if(candidate.toInstant().toEpochMilli()>now && (mask==0||(mask&(1<<bit))!=0))return candidate.toInstant().toEpochMilli();
  }
  throw new IllegalArgumentException("No matching weekday");
 }
 public static long clipped(long start,long end,long from,long to){return Math.max(0,Math.min(end,to)-Math.max(start,from));}
 public static final class IntervalResult {
  public final int fired;public final long due;public final boolean enabled;
  IntervalResult(int fired,long due,boolean enabled){this.fired=fired;this.due=due;this.enabled=enabled;}
 }
 public static IntervalResult advanceInterval(int fired,int total,long due,long now,int minutes){
  if(fired<0||fired>=total||total<1||total>10000||minutes<1||minutes>1440)throw new IllegalArgumentException("Invalid interval state");
  int count=fired+1;boolean enabled=count<total;return new IntervalResult(count,enabled?intervalNext(due,now,minutes):0,enabled);
 }
 public static long intervalNext(long due,long now,int minutes){long next=due+minutes*60000L;return next>now?next:now+minutes*60000L;}
}
