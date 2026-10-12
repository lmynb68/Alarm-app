package com.lmynb68.alarm;
import android.app.*;
import android.content.*;
final class Errors {
 static void notify(Context c,Exception e){
  c.getSharedPreferences("prefs",0).edit().putString("error",e.getMessage()==null?e.toString():e.getMessage()).apply();
  if(android.os.Build.VERSION.SDK_INT>=33&&c.checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS)!=android.content.pm.PackageManager.PERMISSION_GRANTED)return;
  try{NotificationManager m=c.getSystemService(NotificationManager.class);m.createNotificationChannel(new NotificationChannel("errors",Text.t(c,"Alarm needs attention","闹钟需要处理"),NotificationManager.IMPORTANCE_HIGH));
   PendingIntent p=PendingIntent.getActivity(c,0,new Intent(c,MainActivity.class),PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
   m.notify(700,new Notification.Builder(c,"errors").setSmallIcon(com.lmynb68.alarm.R.drawable.ic_alarm).setContentTitle(Text.t(c,"Alarm needs attention","闹钟需要处理")).setContentText(e.getMessage()).setContentIntent(p).setAutoCancel(true).build());
  }catch(Exception ignored){}
 }
}
