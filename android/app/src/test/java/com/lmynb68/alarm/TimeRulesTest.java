package com.lmynb68.alarm;
import org.junit.Test;
import java.time.*;
import static org.junit.Assert.*;
public class TimeRulesTest {
 long epoch(String value){return Instant.parse(value).toEpochMilli();}
 @Test public void pastOrEqualTimeMovesToTomorrow(){long now=epoch("2026-08-27T08:00:00Z");assertEquals(epoch("2026-08-28T08:00:00Z"),TimeRules.next(now,8,0,0,0,ZoneId.of("UTC")));}
 @Test public void weekdaysUseSundayBitZero(){long now=epoch("2026-08-27T08:00:00Z");assertEquals(epoch("2026-08-30T09:00:00Z"),TimeRules.next(now,9,0,0,1,ZoneId.of("UTC")));}
 @Test public void sameDayFutureKeepsSeconds(){long now=epoch("2026-08-27T08:00:00Z");assertEquals(epoch("2026-08-27T08:00:01Z"),TimeRules.next(now,8,0,1,0,ZoneId.of("UTC")));}
 @Test public void crossesMonthWithoutDoubleCounting(){long start=epoch("2026-08-31T23:55:00Z"),end=epoch("2026-09-01T00:05:00Z"),boundary=epoch("2026-09-01T00:00:00Z");assertEquals(300000,TimeRules.clipped(start,end,start,boundary));assertEquals(300000,TimeRules.clipped(start,end,boundary,end));assertEquals(0,TimeRules.clipped(start,end,end,end+60000));}
 @Test public void missedIntervalDoesNotReplayBurst(){long due=epoch("2026-08-27T08:00:00Z"),now=epoch("2026-08-27T08:30:00Z");assertEquals(now+300000,TimeRules.intervalNext(due,now,5));}
 @Test public void normalIntervalStaysOnCadence(){long due=epoch("2026-08-27T08:00:00Z");assertEquals(due+300000,TimeRules.intervalNext(due,due+1000,5));}
 @Test public void springDstGapIsValidLocalTime(){ZoneId zone=ZoneId.of("America/New_York");long now=epoch("2026-03-08T06:00:00Z");assertEquals(epoch("2026-03-08T07:30:00Z"),TimeRules.next(now,2,30,0,0,zone));}
 @Test public void intervalStopsExactlyAfterTwelve(){int fired=0;long due=epoch("2026-08-27T08:05:00Z");for(int n=1;n<=12;n++){TimeRules.IntervalResult r=TimeRules.advanceInterval(fired,12,due,due,5);assertEquals(n,r.fired);assertEquals(n<12,r.enabled);if(n<12)assertEquals(due+300000,r.due);else assertEquals(0,r.due);fired=r.fired;due=r.due;}}
 @Test public void oneIntervalCompletesOnFirstRing(){TimeRules.IntervalResult r=TimeRules.advanceInterval(0,1,1000,1000,5);assertFalse(r.enabled);assertEquals(1,r.fired);assertEquals(0,r.due);}
 @Test(expected=IllegalArgumentException.class) public void completedIntervalCannotFireAgain(){TimeRules.advanceInterval(12,12,0,1000,5);}
}
