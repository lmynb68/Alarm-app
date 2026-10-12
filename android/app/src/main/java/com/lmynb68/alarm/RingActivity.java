package com.lmynb68.alarm;
import android.app.Activity;
import android.os.*;
import android.widget.*;
public final class RingActivity extends Activity {
 String renderedState="";
 final Handler handler=new Handler(Looper.getMainLooper());
 final Runnable tick=new Runnable(){public void run(){String state=RingService.active.keySet().toString()+Text.chinese(RingActivity.this);if(RingService.active.isEmpty()||!state.equals(renderedState)){renderedState=state;render();}handler.postDelayed(this,500);}};
 @Override public void onCreate(Bundle b){super.onCreate(b);if(Build.VERSION.SDK_INT>=27){setShowWhenLocked(true);setTurnScreenOn(true);}else{getWindow().addFlags(android.view.WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED|android.view.WindowManager.LayoutParams.FLAG_TURN_SCREEN_ON);}}
 @Override public void onResume(){super.onResume();tick.run();}
 @Override public void onPause(){handler.removeCallbacks(tick);super.onPause();}
 void render(){if(RingService.active.isEmpty()){finish();return;}LinearLayout root=new LinearLayout(this);root.setOrientation(LinearLayout.VERTICAL);root.setPadding(32,64,32,32);root.setBackgroundColor(0xfff3f7fe);TextView title=new TextView(this);title.setText(Text.t(this,"Alarm ringing","闹钟响铃"));title.setTextSize(30);root.addView(title);
  java.util.HashSet<String> shown=new java.util.HashSet<>();for(Alarm a:RingService.active.values()){if(!shown.add(a.id))continue;TextView name=new TextView(this);name.setText(a.name);name.setTextSize(24);root.addView(name);Button stop=new Button(this);stop.setText(Text.t(this,"Stop","停止"));stop.setOnClickListener(v->{RingService.stopFor(this,a.id,false);});root.addView(stop);Button later=new Button(this);later.setText(Text.t(this,"Snooze 5 min","5 分钟后提醒"));later.setOnClickListener(v->{RingService.stopFor(this,a.id,true);});root.addView(later);}ScrollView scroll=new ScrollView(this);scroll.addView(root);setContentView(scroll);
 }
}
