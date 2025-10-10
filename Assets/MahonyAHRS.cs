using System;

public class MahonyAHRS
{
    public float[] Quaternion = new float[4] { 1f, 0f, 0f, 0f };
    private float samplePeriod;
    private float kp;
    private float ki;
    private float[] integralFB = new float[3] { 0f, 0f, 0f };

    public MahonyAHRS(float kp, float ki, float samplePeriod)
    {
        this.kp = kp;
        this.ki = ki;
        this.samplePeriod = samplePeriod;
    }

    public void Update(float gx, float gy, float gz, float ax, float ay, float az)
    {
        // 6DOF implementation (without magnetometer)
        float q1 = Quaternion[0], q2 = Quaternion[1], q3 = Quaternion[2], q4 = Quaternion[3];

        // Normalize accelerometer measurement
        float norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
        if (norm == 0f) return;
        ax /= norm; ay /= norm; az /= norm;

        // Estimated direction of gravity
        float vx = 2.0f * (q2 * q4 - q1 * q3);
        float vy = 2.0f * (q1 * q2 + q3 * q4);
        float vz = q1 * q1 - q2 * q2 - q3 * q3 + q4 * q4;

        // Error is cross product between estimated and measured direction of gravity
        float ex = (ay * vz - az * vy);
        float ey = (az * vx - ax * vz);
        float ez = (ax * vy - ay * vx);

        // Integral error
        if (ki > 0f)
        {
            integralFB[0] += ex * ki * samplePeriod;
            integralFB[1] += ey * ki * samplePeriod;
            integralFB[2] += ez * ki * samplePeriod;

            // Apply integral feedback
            gx += integralFB[0];
            gy += integralFB[1];
            gz += integralFB[2];
        }
        else
        {
            integralFB[0] = 0f; integralFB[1] = 0f; integralFB[2] = 0f;
        }

        // Apply proportional feedback
        gx += kp * ex;
        gy += kp * ey;
        gz += kp * ez;

        // Integrate rate of change of quaternion
        gx *= 0.5f * samplePeriod;
        gy *= 0.5f * samplePeriod;
        gz *= 0.5f * samplePeriod;

        float qa = q1;
        float qb = q2;
        float qc = q3;

        q1 += (-qb * gx - qc * gy - q4 * gz);
        q2 += (qa * gx + qc * gz - q4 * gy);
        q3 += (qa * gy - qb * gz + q4 * gx);
        q4 += (qa * gz + qb * gy - qc * gx);

        // Normalize quaternion
        norm = (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);
        if (norm == 0f) return;

        Quaternion[0] = q1 / norm;
        Quaternion[1] = q2 / norm;
        Quaternion[2] = q3 / norm;
        Quaternion[3] = q4 / norm;
    }

    public void Update(float gx, float gy, float gz, float ax, float ay, float az, float mx, float my, float mz)
    {
        // 9DOF implementation (with magnetometer)
        float q1 = Quaternion[0], q2 = Quaternion[1], q3 = Quaternion[2], q4 = Quaternion[3];

        // Normalize accelerometer measurement
        float norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
        if (norm == 0f) return;
        ax /= norm; ay /= norm; az /= norm;

        // Normalize magnetometer measurement
        norm = (float)Math.Sqrt(mx * mx + my * my + mz * mz);
        if (norm == 0f) return;
        mx /= norm; my /= norm; mz /= norm;

        // Reference direction of Earth's magnetic field
        float hx = 2f * mx * (0.5f - q3 * q3 - q4 * q4) + 2f * my * (q2 * q3 - q1 * q4) + 2f * mz * (q2 * q4 + q1 * q3);
        float hy = 2f * mx * (q2 * q3 + q1 * q4) + 2f * my * (0.5f - q2 * q2 - q4 * q4) + 2f * mz * (q3 * q4 - q1 * q2);
        float hz = 2f * mx * (q2 * q4 - q1 * q3) + 2f * my * (q3 * q4 + q1 * q2) + 2f * mz * (0.5f - q2 * q2 - q3 * q3);

        float bx = (float)Math.Sqrt(hx * hx + hy * hy);
        float bz = hz;

        // Estimated direction of gravity and magnetic field
        float vx = 2f * (q2 * q4 - q1 * q3);
        float vy = 2f * (q1 * q2 + q3 * q4);
        float vz = q1 * q1 - q2 * q2 - q3 * q3 + q4 * q4;

        float wx = 2f * bx * (0.5f - q3 * q3 - q4 * q4) + 2f * bz * (q2 * q4 - q1 * q3);
        float wy = 2f * bx * (q2 * q3 - q1 * q4) + 2f * bz * (q1 * q2 + q3 * q4);
        float wz = 2f * bx * (q1 * q3 + q2 * q4) + 2f * bz * (0.5f - q2 * q2 - q3 * q3);

        // Error is cross product between estimated direction and measured direction of fields
        float ex = (ay * vz - az * vy) + (my * wz - mz * wy);
        float ey = (az * vx - ax * vz) + (mz * wx - mx * wz);
        float ez = (ax * vy - ay * vx) + (mx * wy - my * wx);

        // Integral error
        if (ki > 0f)
        {
            integralFB[0] += ex * ki * samplePeriod;
            integralFB[1] += ey * ki * samplePeriod;
            integralFB[2] += ez * ki * samplePeriod;

            // Apply integral feedback
            gx += integralFB[0];
            gy += integralFB[1];
            gz += integralFB[2];
        }
        else
        {
            integralFB[0] = 0f; integralFB[1] = 0f; integralFB[2] = 0f;
        }

        // Apply proportional feedback
        gx += kp * ex;
        gy += kp * ey;
        gz += kp * ez;

        // Integrate rate of change of quaternion
        gx *= 0.5f * samplePeriod;
        gy *= 0.5f * samplePeriod;
        gz *= 0.5f * samplePeriod;

        float qa = q1;
        float qb = q2;
        float qc = q3;

        q1 += (-qb * gx - qc * gy - q4 * gz);
        q2 += (qa * gx + qc * gz - q4 * gy);
        q3 += (qa * gy - qb * gz + q4 * gx);
        q4 += (qa * gz + qb * gy - qc * gx);

        // Normalize quaternion
        norm = (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);
        if (norm == 0f) return;

        Quaternion[0] = q1 / norm;
        Quaternion[1] = q2 / norm;
        Quaternion[2] = q3 / norm;
        Quaternion[3] = q4 / norm;
    }

    public void ResetIntegral()
    {
        integralFB[0] = 0f;
        integralFB[1] = 0f;
        integralFB[2] = 0f;
    }

    public float[] GetEulerAngles()
    {
        float q0 = Quaternion[0], q1 = Quaternion[1], q2 = Quaternion[2], q3 = Quaternion[3];

        float[] euler = new float[3];

        // Roll (x-axis rotation)
        float sinr_cosp = 2f * (q0 * q1 + q2 * q3);
        float cosr_cosp = 1f - 2f * (q1 * q1 + q2 * q2);
        euler[0] = (float)Math.Atan2(sinr_cosp, cosr_cosp);

        // Pitch (y-axis rotation)
        float sinp = 2f * (q0 * q2 - q3 * q1);
        if (Math.Abs(sinp) >= 1f)
            euler[1] = (float)((sinp > 0f ? 1f : -1f) * (Math.PI / 2f));
        else
            euler[1] = (float)Math.Asin(sinp);

        // Yaw (z-axis rotation)
        float siny_cosp = 2f * (q0 * q3 + q1 * q2);
        float cosy_cosp = 1f - 2f * (q2 * q2 + q3 * q3);
        euler[2] = (float)Math.Atan2(siny_cosp, cosy_cosp);

        return euler;
    }
}