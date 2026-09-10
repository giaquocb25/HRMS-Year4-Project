import React, {useRef, useState} from 'react';
import {
  Alert,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import QRCodeScanner from 'react-native-qrcode-scanner';

export default function SettingScreen({navigation}) {
  const [ip, setIp] = useState('');
  const [token, setToken] = useState('');
  const [port, setPort] = useState('8080');
  const qr = useRef(null);

  const onConfirm = () => {
    const normalizedIp = ip
      .trim()
      .replace(/^https?:\/\//i, '')
      .replace(/[:/].*$/, '');
    const normalizedToken = token.trim();

    const normalizedPort = Number.parseInt(port, 10);
    if (
      !normalizedIp ||
      !normalizedToken ||
      !Number.isInteger(normalizedPort) ||
      normalizedPort < 1024 ||
      normalizedPort > 65535
    ) {
      Alert.alert(
        'Thiếu thông tin',
        'Hãy quét mã thiết lập hoặc nhập đủ địa chỉ IP, cổng và token.',
      );
      return;
    }

    navigation.navigate('QRScanner', {
      ip: normalizedIp,
      token: normalizedToken,
      port: normalizedPort,
    });
  };

  const onSuccess = event => {
    const data = String(event.data || '').split(',');
    if (
      (data.length !== 2 && data.length !== 3) ||
      !data[0].trim() ||
      !data[1].trim()
    ) {
      Alert.alert(
        'Mã không hợp lệ',
        'Đây không phải mã thiết lập chấm công của HRMS.',
      );
      qr.current?.reactivate();
      return;
    }

    setToken(data[0].trim());
    setIp(data[1].trim());
    setPort(data.length === 3 ? data[2].trim() : '8080');
    setTimeout(() => qr.current?.reactivate(), 1500);
  };

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <QRCodeScanner
        containerStyle={styles.qrContainer}
        ref={node => {
          qr.current = node;
        }}
        onRead={onSuccess}
        topContent={
          <Text style={styles.heading}>Quét mã thiết lập trên máy quản lý</Text>
        }
        topViewStyle={styles.topView}
        bottomViewStyle={styles.bottomView}
        cameraContainerStyle={styles.cameraContainer}
        cameraStyle={styles.camera}
      />

      <View>
        <Text style={styles.text}>Địa chỉ IP máy quản lý</Text>
        <TextInput
          style={styles.input}
          onChangeText={setIp}
          value={ip}
          autoCapitalize="none"
          keyboardType="numeric"
          placeholder="Ví dụ 192.168.1.10"
        />
        <Text style={styles.text}>Token phiên chấm công</Text>
        <TextInput
          style={styles.input}
          onChangeText={setToken}
          value={token}
          autoCapitalize="none"
          placeholder="Token được điền tự động khi quét"
        />
        <Text style={styles.text}>Cổng dịch vụ</Text>
        <TextInput
          style={styles.input}
          onChangeText={setPort}
          value={port}
          keyboardType="numeric"
          placeholder="8080"
        />
      </View>

      <Pressable style={styles.button} onPress={onConfirm}>
        <Text style={styles.buttonText}>Bắt đầu quét nhân viên</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    padding: 12,
    paddingBottom: 40,
  },
  qrContainer: {
    flex: 1,
    marginTop: 10,
  },
  heading: {
    flex: 1,
    color: '#111827',
    fontSize: 20,
    fontWeight: '600',
    textAlign: 'center',
  },
  topView: {
    height: 52,
    flex: 0,
  },
  bottomView: {
    flex: 1,
    marginTop: 10,
  },
  cameraContainer: {
    alignItems: 'center',
    height: 360,
    margin: 0,
  },
  camera: {
    flex: 1,
    width: 300,
    margin: 0,
  },
  text: {
    color: '#111827',
    fontSize: 16,
    fontWeight: '600',
    marginTop: 10,
  },
  input: {
    backgroundColor: '#fff',
    marginVertical: 5,
    width: '100%',
    color: '#000',
    borderColor: '#D1D5DB',
    borderWidth: 1,
    borderRadius: 6,
    paddingHorizontal: 10,
  },
  button: {
    minWidth: 220,
    height: 44,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#2563EB',
    alignSelf: 'center',
    marginTop: 14,
    borderRadius: 6,
    paddingHorizontal: 16,
  },
  buttonText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 16,
  },
});
