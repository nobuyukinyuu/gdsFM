shader_type canvas_item;

uniform vec4 color: hint_color = vec4(1.0,1.0,0.0, 1.0);
uniform float curve : hint_range(-4.0, 5.0) = 1.0;
uniform float thickness : hint_range(1.0, 10.0) = 1.0;
uniform bool flip_h = false;
uniform float sl : hint_range(0.0, 1.0);
uniform float tl : hint_range(0.0, 1.0) = 1.0;

const float PI = 3.14159265359;

//Godot easing func.
float ease(float p_x, float p_c) {
	if (p_x < 0.0)
		p_x = 0.0;
	else if (p_x > 1.0)
		p_x = 1.0;
	if (p_c > 0.0) {
		if (p_c < 1.0) {
			return 1.0 - pow(1.0 - p_x, 1.0 / p_c);
		} else {
			return pow(p_x, p_c);
		}
	} else if (p_c < 0.0) {
		//inout ease

		if (p_x < 0.5) {
			return pow(p_x * 2.0, -p_c) * 0.5;
		} else {
			return (1.0 - pow(1.0 - (p_x - 0.5) * 2.0, -p_c)) * 0.5 + 0.5;
		}
	} else
		return (tl+sl)/2.0; // no ease (raw)
}

float thresh(float n, float low, float high)
{
	if (n <=0.0) return 0.0;
	if (n >=1.0) return 1.0;
	return 1.0;
}

// Plot a line on Y using a value between 0.0-1.0
float plot(vec2 st, float pct){
	float t = thickness * 0.03;
  return  thresh( pct-t*0.0, pct, st.y) -
          smoothstep( pct, pct+t, st.y);
}

void fragment() {
	vec2 st = UV;
//	st.x *= sl;
	


	if (st.y < 0.5)  st.y = 1.0 - st.y;
	st.y *=2.0;
	st.y -= 1.0;


	st.y -= sl * tl;
	if (sl>0.0) {st.y /= 1.0-sl;} else {}


	if (tl>0.0) {st.y /= tl} else {st.y = 1.0;} //Adjust total level
	
	
	if (flip_h) st.x = 1.0 - st.x;

    float y = ease(st.x, flip_h? ((curve==0.0)? 0.0: 1.0/curve) : curve);

    vec4 c = vec4(0);

    // Plot a line
    float pct = plot(st,y);
    c = (1.0-pct)*c+pct * color;
	
	COLOR = c;
}