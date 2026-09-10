import React from 'react';
import {NavigationContainer} from '@react-navigation/native';
import {createNativeStackNavigator} from '@react-navigation/native-stack';
import SettingScreen from './src/screen/SettingScreen';
import QRScannerScreen from './src/screen/QRScannerScreen';

type RootStackParamList = {
  Setting: undefined;
  QRScanner: {ip: string; token: string};
};

const Stack = createNativeStackNavigator<RootStackParamList>();

function App(): JSX.Element {
  return (
    <NavigationContainer>
      <Stack.Navigator>
        <Stack.Screen
          name="Setting"
          component={SettingScreen}
          options={{title: 'Thiết lập chấm công'}}
        />
        <Stack.Screen
          name="QRScanner"
          component={QRScannerScreen}
          options={({route}) => ({title: `Máy chủ: ${route.params.ip}`})}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}

export default App;
