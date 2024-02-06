package util;

import java.util.Objects;


public class Pair<T1,T2> {
    public T1 m_a;
    public T2 m_b;

    public Pair(T1 a,T2 b) {
        m_a = a;
        m_b = b;
    }

    @Override
    public String toString() {
        return "<" + m_a + "," + m_b + ">";
    }
    
    @Override
    public boolean equals(Object o) {
        if (this == o)
            return true;
        if (o == null || getClass() != o.getClass())
            return false;
        Pair<T1, T2> that = (Pair<T1, T2>) o;
        return m_a == that.m_a && m_b == that.m_b;
    }
    
    @Override
    public int hashCode() {
        return Objects.hash(m_a, m_b);
    }
}
