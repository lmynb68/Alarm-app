package com.lmynb68.alarm;
import android.app.*;
import android.content.*;
import android.media.*;
import android.net.Uri;
import android.os.*;
import java.util.*;
public final class RingService extends Service {
 static RingService instance;
 static void refreshVolume(){if(instance!=null)Audio.applyVolume(instance,instance.player);}
 static final LinkedHashMap<String,Alarm> active=new LinkedHashMap<>();
 MediaPlayer player;PowerManager.WakeLock wake;Vibrator vibrator;AudioManager audio;AudioFocusRequest focus;
 final Handler handler=new Handler(Looper.getMainLooper());
 final Runnable checkpoint=new Runnable(){public void run(){try{synchronized(Store.class){Store.Data d=Store.load(RingService.this);long now=System.currentTimeMillis();for(int n=0;n<d.history.length();n++){org.json.JSONObject j=d.history.optJSONObject(n);if(active.containsKey(j.optString("token"))&&j.optBoolean("open"))Store.put(j,"end",now);}Store.save(RingService.this,d);}if(wake!=null){if(wake.isHeld())wake.release();wake.acquire(60000);}}catch(Exception e){Errors.notify(RingService.this,e);}handler.postDelayed(this,30000);}};
 static Alarm latest(){Alarm result=null;for(Alarm a:active.values())result=a;return result;}
 static void stopFor(Context c,String id,boolean snooze){for(Alarm a:active.values())if(a.id.equals(id)){c.startService(new Intent(c,RingService.class).setAction(snooze?"SNOOZE":"STOP").putExtra("id",id));break;}}
 @Override public void onCreate(){super.onCreate();instance=this;NotificationManager m=getSystemService(NotificationManager.class);NotificationChannel ch=new NotificationChannel("ring-v1",Text.t(this,"Alarm ringing","闹钟响铃"),NotificationManager.IMPORTANCE_HIGH);ch.setSound(null,null);ch.enableVibration(false);ch.setLockscreenVisibility(Notification.VISIBILITY_PUBLIC);m.createNotificationChannel(ch);wake=getSystemService(PowerManager.class).newWakeLock(PowerManager.PARTIAL_WAKE_LOCK,"alarmapp:ring");vibrator=getSystemService(Vibrator.class);audio=getSystemService(AudioManager.class);focus=new AudioFocusRequest.Builder(AudioManager.AUDIOFOCUS_GAIN_TRANSIENT).setAudioAttributes(new AudioAttributes.Builder().setUsage(AudioAttributes.USAGE_ALARM).build()).setAcceptsDelayedFocusGain(true).setOnAudioFocusChangeListener(change->{if(player!=null){if(change==AudioManager.AUDIOFOCUS_LOSS_TRANSIENT||change==AudioManager.AUDIOFOCUS_LOSS)player.pause();else if(change==AudioManager.AUDIOFOCUS_GAIN)player.start();}}).build();}
 @Override public int onStartCommand(Intent i,int flags,int startId){
  if(i==null){stopSelf();return START_NOT_STICKY;}
  String id=i.getStringExtra("id");
  try{
   if("STOP".equals(i.getAction())||"SNOOZE".equals(i.getAction())){
    boolean snooze="SNOOZE".equals(i.getAction());
    synchronized(Store.class){Store.Data d=Store.load(this);long now=System.currentTimeMillis();Alarm a=Store.find(d,id);
     if(snooze&&a!=null){if(!Scheduler.allowed(this))throw new SecurityException("Exact alarm permission required");a.snooze=now+300000;Scheduler.schedule(this,a);}
     Iterator<Map.Entry<String,Alarm>> it=active.entrySet().iterator();while(it.hasNext()){Map.Entry<String,Alarm> entry=it.next();if(entry.getValue().id.equals(id)){Store.closeToken(d,entry.getKey(),now);it.remove();}}
     Store.save(this,d);
    }
    releasePlayer();if(active.isEmpty()){stopForeground(STOP_FOREGROUND_REMOVE);stopSelf();}else {show();play(latest());}
   }else{
    String token=i.getStringExtra("token");if(token==null||active.containsKey(token))return START_NOT_STICKY;
    synchronized(Store.class){Store.Data d=Store.load(this);Alarm a=Store.find(d,id);if(a==null){stopSelf();return START_NOT_STICKY;}active.put(token,a);Store.open(d,a,"Ringing",token,System.currentTimeMillis());Store.save(this,d);}
    show();play(latest());if(!wake.isHeld())wake.acquire(60000);handler.removeCallbacks(checkpoint);handler.postDelayed(checkpoint,30000);
   }
  }catch(Exception e){Errors.notify(this,e);if(!active.isEmpty()){try{show();}catch(Exception ignored){} }else stopSelf();}
  return START_NOT_STICKY;
 }
 void show(){Alarm a=latest();if(a==null)return;
  Intent screen=new Intent(this,RingActivity.class).putExtra("id",a.id).setFlags(Intent.FLAG_ACTIVITY_NEW_TASK|Intent.FLAG_ACTIVITY_CLEAR_TOP);
  PendingIntent open=PendingIntent.getActivity(this,10,screen,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
  PendingIntent stop=action(a,"STOP",11),later=action(a,"SNOOZE",12);
  Notification.Builder b=new Notification.Builder(this,"ring-v1").setSmallIcon(R.drawable.ic_alarm).setContentTitle(a.name).setContentText(Text.t(this,"Alarm ringing • tap to open","正在响铃 · 点击打开")+" ("+active.size()+")").setContentIntent(open).setCategory(Notification.CATEGORY_ALARM).setVisibility(Notification.VISIBILITY_PUBLIC).setOngoing(true).setOnlyAlertOnce(true).addAction(new Notification.Action.Builder(null,Text.t(this,"Stop","停止"),stop).build()).addAction(new Notification.Action.Builder(null,Text.t(this,"Snooze 5 min","5 分钟后提醒"),later).build());
  NotificationManager manager=getSystemService(NotificationManager.class);if(Build.VERSION.SDK_INT<34||manager.canUseFullScreenIntent())b.setFullScreenIntent(open,true);
  startForeground(101,b.build());
 }
 PendingIntent action(Alarm a,String action,int code){Intent i=new Intent(this,RingService.class).setAction(action).setData(Uri.parse("alarmapp://action/"+action+"/"+a.id)).putExtra("id",a.id);return PendingIntent.getService(this,code,i,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);}
 void play(Alarm a){releasePlayer();int focusResult=audio.requestAudioFocus(focus);try{player=Audio.create(this,a.sound);if(focusResult==AudioManager.AUDIOFOCUS_REQUEST_GRANTED)player.start();}catch(Exception e){Errors.notify(this,e);try{player=Audio.create(this,"classic");if(focusResult==AudioManager.AUDIOFOCUS_REQUEST_GRANTED)player.start();}catch(Exception failure){Errors.notify(this,failure);}}
  if(vibrator!=null&&vibrator.hasVibrator())vibrator.vibrate(VibrationEffect.createWaveform(new long[]{0,500,500},0),new AudioAttributes.Builder().setUsage(AudioAttributes.USAGE_ALARM).build());
 }
 void releasePlayer(){if(player!=null){player.release();player=null;}if(vibrator!=null)vibrator.cancel();}
 @Override public void onDestroy(){instance=null;handler.removeCallbacksAndMessages(null);releasePlayer();if(audio!=null&&focus!=null)audio.abandonAudioFocusRequest(focus);if(wake!=null&&wake.isHeld())wake.release();try{synchronized(Store.class){Store.Data d=Store.load(this);for(String token:active.keySet())Store.closeToken(d,token,System.currentTimeMillis());Store.save(this,d);}}catch(Exception e){Errors.notify(this,e);}active.clear();stopForeground(STOP_FOREGROUND_REMOVE);super.onDestroy();}
 @Override public IBinder onBind(Intent i){return null;}
}
