package com.lmynb68.alarm;
import android.content.Context;
import android.graphics.*;
import android.view.View;
final class NavIcon extends View {
 final int kind,color;final Paint p=new Paint(Paint.ANTI_ALIAS_FLAG);
 NavIcon(Context c,int kind,int color){super(c);this.kind=kind;this.color=color;setImportantForAccessibility(IMPORTANT_FOR_ACCESSIBILITY_NO);}
 @Override protected void onDraw(Canvas c){super.onDraw(c);c.save();float unit=Math.min(getWidth(),getHeight())/24f;c.translate((getWidth()-24*unit)/2,(getHeight()-24*unit)/2);c.scale(unit,unit);p.setColor(color);p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(1.8f);p.setStrokeCap(Paint.Cap.ROUND);p.setStrokeJoin(Paint.Join.ROUND);
 if(kind==0){c.drawCircle(12,13,7,p);c.drawLine(12,9,12,13,p);c.drawLine(12,13,15,15,p);c.drawLine(5,3,2,6,p);c.drawLine(19,3,22,6,p);c.drawLine(7,20,5,22,p);c.drawLine(17,20,19,22,p);}
 else if(kind==3){c.drawCircle(12,12,9,p);c.drawOval(7,3,17,21,p);c.drawLine(3,12,21,12,p);c.drawLine(5,7,19,7,p);c.drawLine(5,17,19,17,p);}
 else if(kind==4){c.drawCircle(12,14,7,p);c.drawLine(9,2,15,2,p);c.drawLine(12,2,12,5,p);c.drawLine(18,6,20,4,p);c.drawLine(12,10,12,14,p);c.drawLine(12,14,15,16,p);}
 else{c.drawRoundRect(3,3,21,21,2,2,p);if(kind==1){c.drawLine(7,16,7,12,p);c.drawLine(12,16,12,7,p);c.drawLine(17,16,17,10,p);}else{for(int y=7;y<=17;y+=5){c.drawLine(7,y,8,y,p);c.drawLine(12,y,17,y,p);}}}c.restore();}
}
