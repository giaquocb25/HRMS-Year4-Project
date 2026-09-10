import React, {useRef} from 'react';
import {StyleSheet, Text} from 'react-native';
import QRCodeScanner from 'react-native-qrcode-scanner';

export default function TokenScannerScreen({navigation}) {
  const qr = useRef(null);

  const onSuccess = event => {
    navigation.navigate('Setting', {token: event.data});
  };

  return (
    <QRCodeScanner
      ref={node => {
        qr.current = node;
      }}
      onRead={onSuccess}
      topContent={
        <Text style={styles.centerText}>
          Quét mã thiết lập hiển thị trên máy quản lý
        </Text>
      }
    />
  );
}

const styles = StyleSheet.create({
  centerText: {
    flex: 1,
    fontSize: 18,
    padding: 32,
    color: '#374151',
    textAlign: 'center',
  },
});
